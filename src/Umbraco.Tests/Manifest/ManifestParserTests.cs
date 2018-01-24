﻿using System;
using System.Linq;
using Moq;
using System.Text;
using NUnit.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Manifest;
using Umbraco.Core.PropertyEditors;

namespace Umbraco.Tests.Manifest
{
    [TestFixture]
    public class ManifestParserTests
    {

        private ManifestParser _parser;

        [SetUp]
        public void Setup()
        {
            _parser = new ManifestParser(NullCacheProvider.Instance, Mock.Of<ILogger>());
        }

        [Test]
        public void CanParseComments()
        {

            const string json1 = @"
// this is a single-line comment
{
    ""x"": 2, // this is an end-of-line comment
    ""y"": 3, /* this is a single line comment block
/* comment */ ""z"": /* comment */ 4,
    ""t"": ""this is /* comment */ a string"",
    ""u"": ""this is // more comment in a string""
}
";

            var jobject = (JObject) JsonConvert.DeserializeObject(json1);
            Assert.AreEqual("2", jobject.Property("x").Value.ToString());
            Assert.AreEqual("3", jobject.Property("y").Value.ToString());
            Assert.AreEqual("4", jobject.Property("z").Value.ToString());
            Assert.AreEqual("this is /* comment */ a string", jobject.Property("t").Value.ToString());
            Assert.AreEqual("this is // more comment in a string", jobject.Property("u").Value.ToString());
        }

        [Test]
        public void ThrowOnJsonError()
        {
            // invalid json, missing the final ']' on javascript
            const string json = @"{
propertyEditors: []/*we have empty property editors**/,
javascript: ['~/test.js',/*** some note about stuff asd09823-4**09234*/ '~/test2.js' }";

            // parsing fails
            Assert.Throws<JsonReaderException>(() => _parser.ParseManifest(json));
        }

        [Test]
        public void CanParseManifest_ScriptsAndStylesheets()
        {
            var json = "{}";
            var manifest = _parser.ParseManifest(json);
            Assert.AreEqual(0, manifest.Scripts.Length);

            json = "{javascript: []}";
            manifest = _parser.ParseManifest(json);
            Assert.AreEqual(0, manifest.Scripts.Length);

            json = "{javascript: ['~/test.js', '~/test2.js']}";
            manifest = _parser.ParseManifest(json);
            Assert.AreEqual(2, manifest.Scripts.Length);

            json = "{propertyEditors: [], javascript: ['~/test.js', '~/test2.js']}";
            manifest = _parser.ParseManifest(json);
            Assert.AreEqual(2, manifest.Scripts.Length);

            Assert.AreEqual("/test.js", manifest.Scripts[0]);
            Assert.AreEqual("/test2.js", manifest.Scripts[1]);

            // kludge is gone - must filter before parsing
            json = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble()) + "{propertyEditors: [], javascript: ['~/test.js', '~/test2.js']}";
            Assert.Throws<JsonReaderException>(() => _parser.ParseManifest(json));

            json = "{}";
             manifest = _parser.ParseManifest(json);
            Assert.AreEqual(0, manifest.Stylesheets.Length);

            json = "{css: []}";
            manifest = _parser.ParseManifest(json);
            Assert.AreEqual(0, manifest.Stylesheets.Length);

            json = "{css: ['~/style.css', '~/folder-name/sdsdsd/stylesheet.css']}";
            manifest = _parser.ParseManifest(json);
            Assert.AreEqual(2, manifest.Stylesheets.Length);

            json = "{propertyEditors: [], css: ['~/stylesheet.css', '~/random-long-name.css']}";
            manifest = _parser.ParseManifest(json);
            Assert.AreEqual(2, manifest.Stylesheets.Length);



            json = "{propertyEditors: [], javascript: ['~/test.js', '~/test2.js'], css: ['~/stylesheet.css', '~/random-long-name.css']}";
            manifest = _parser.ParseManifest(json);
            Assert.AreEqual(2, manifest.Scripts.Length);
            Assert.AreEqual(2, manifest.Stylesheets.Length);
        }

        [Test]
        public void CanParseManifest_PropertyEditors()
        {
            const string json = @"{'propertyEditors': [
    {
        alias: 'Test.Test1',
        name: 'Test 1',
        editor: {
            view: '~/App_Plugins/MyPackage/PropertyEditors/MyEditor.html',
            valueType: 'int',
            hideLabel: true,
            validation: {
                'required': true,
                'Regex': '\\d*'
            }
        },
        prevalues: {
                fields: [
                    {
                        label: 'Some config 1',
                        key: 'key1',
                        view: '~/App_Plugins/MyPackage/PropertyEditors/Views/pre-val1.html',
                        validation: {
                            required: true
                        }
                    },
                    {
                        label: 'Some config 2',
                        key: 'key2',
                        view: '~/App_Plugins/MyPackage/PropertyEditors/Views/pre-val2.html'
                    }
                ]
            }
    },
    {
        alias: 'Test.Test2',
        name: 'Test 2',
        isParameterEditor: true,
        defaultConfig: { key1: 'some default val' },
        editor: {
            view: '~/App_Plugins/MyPackage/PropertyEditors/MyEditor.html',
            valueType: 'int',
            validation: {
                required : true,
                regex : '\\d*'
            }
        }
    }
]}";

            var manifest = _parser.ParseManifest(json);
            Assert.AreEqual(2, manifest.PropertyEditors.Length);

            var editor = manifest.PropertyEditors[1];
            Assert.IsTrue(editor.IsParameterEditor);

            editor = manifest.PropertyEditors[0];
            Assert.AreEqual("Test.Test1", editor.Alias);
            Assert.AreEqual("Test 1", editor.Name);
            Assert.IsFalse(editor.IsParameterEditor);

            var valueEditor = editor.ValueEditor;
            Assert.AreEqual("/App_Plugins/MyPackage/PropertyEditors/MyEditor.html", valueEditor.View);
            Assert.AreEqual("int", valueEditor.ValueType);
            Assert.IsTrue(valueEditor.HideLabel);

            // these two don't make much sense here
            // valueEditor.RegexValidator;
            // valueEditor.RequiredValidator;

            var validators = valueEditor.Validators;
            Assert.AreEqual(2, validators.Count);
            var validator = validators[0];
            var v = validator as ManifestValueValidator;
            Assert.IsNotNull(v);
            Assert.AreEqual("required", v.ValidationName);
            Assert.AreEqual("", v.Config);
            validator = validators[1];
            v = validator as ManifestValueValidator;
            Assert.IsNotNull(v);
            Assert.AreEqual("Regex", v.ValidationName);
            Assert.AreEqual("\\d*", v.Config);

            // this is not part of the manifest
            var preValues = editor.DefaultPreValues;
            Assert.IsNull(preValues);

            var preValueEditor = editor.ConfigurationEditor;
            Assert.IsNotNull(preValueEditor);
            Assert.IsNotNull(preValueEditor.Fields);
            Assert.AreEqual(2, preValueEditor.Fields.Count);

            var f = preValueEditor.Fields[0];
            Assert.AreEqual("key1", f.Key);
            Assert.AreEqual("Some config 1", f.Name);
            Assert.AreEqual("/App_Plugins/MyPackage/PropertyEditors/Views/pre-val1.html", f.View);
            var fvalidators = f.Validators;
            Assert.IsNotNull(fvalidators);
            Assert.AreEqual(1, fvalidators.Count);
            var fv = fvalidators[0] as ManifestValueValidator;
            Assert.IsNotNull(fv);
            Assert.AreEqual("required", fv.ValidationName);
            Assert.AreEqual("", fv.Config);

            f = preValueEditor.Fields[1];
            Assert.AreEqual("key2", f.Key);
            Assert.AreEqual("Some config 2", f.Name);
            Assert.AreEqual("/App_Plugins/MyPackage/PropertyEditors/Views/pre-val2.html", f.View);
            fvalidators = f.Validators;
            Assert.IsNotNull(fvalidators);
            Assert.AreEqual(0, fvalidators.Count);
        }

        [Test]
        public void CanParseManifest_ParameterEditors()
        {
            const string json = @"{'parameterEditors': [
    {
        alias: 'parameter1',
        name: 'My Parameter',
        view: '~/App_Plugins/MyPackage/PropertyEditors/MyEditor.html'
    },
    {
        alias: 'parameter2',
        name: 'Another parameter',
        config: { key1: 'some config val' },
        view: '~/App_Plugins/MyPackage/PropertyEditors/CsvEditor.html'
    },
    {
        alias: 'parameter3',
        name: 'Yet another parameter'
    }
]}";

            var manifest = _parser.ParseManifest(json);
            Assert.AreEqual(3, manifest.ParameterEditors.Length);

            var editor = manifest.ParameterEditors[1];
            Assert.AreEqual("parameter2", editor.Alias);
            Assert.AreEqual("Another parameter", editor.Name);

            var config = editor.Configuration;
            Assert.AreEqual(1, config.Count);
            Assert.IsTrue(config.ContainsKey("key1"));
            Assert.AreEqual("some config val", config["key1"]);

            var valueEditor = editor.ValueEditor;
            Assert.AreEqual("/App_Plugins/MyPackage/PropertyEditors/CsvEditor.html", valueEditor.View);

            editor = manifest.ParameterEditors[2];
            Assert.Throws<InvalidOperationException>(() =>
            {
                var _ = editor.ValueEditor;
            });
        }

        [Test]
        public void CanParseManifest_GridEditors()
        {
            const string json = @"{
    'javascript': [    ],
    'css': [     ],
    'gridEditors': [
        {
            'name': 'Small Hero',
            'alias': 'small-hero',
            'view': '~/App_Plugins/MyPlugin/small-hero/editortemplate.html',
            'render': '~/Views/Partials/Grid/Editors/SmallHero.cshtml',
            'icon': 'icon-presentation',
            'config': {
                'image': {
                    'size': {
                        'width': 1200,
                        'height': 185
                    }
                },
                'link': {
                    'maxNumberOfItems': 1,
                    'minNumberOfItems': 0
                }
            }
        },
        {
            'name': 'Document Links By Category',
            'alias': 'document-links-by-category',
            'view': '~/App_Plugins/MyPlugin/document-links-by-category/editortemplate.html',
            'render': '~/Views/Partials/Grid/Editors/DocumentLinksByCategory.cshtml',
            'icon': 'icon-umb-members'
        }
    ]
}";
            var manifest = _parser.ParseManifest(json);
            Assert.AreEqual(2, manifest.GridEditors.Length);

            var editor = manifest.GridEditors[0];
            Assert.AreEqual("small-hero", editor.Alias);
            Assert.AreEqual("Small Hero", editor.Name);
            Assert.AreEqual("/App_Plugins/MyPlugin/small-hero/editortemplate.html", editor.View);
            Assert.AreEqual("/Views/Partials/Grid/Editors/SmallHero.cshtml", editor.Render);
            Assert.AreEqual("icon-presentation", editor.Icon);

            var config = editor.Config;
            Assert.AreEqual(2, config.Count);
            Assert.IsTrue(config.ContainsKey("image"));
            var c = config["image"];
            Assert.IsInstanceOf<JObject>(c); // fixme - is this what we want?
            Assert.IsTrue(config.ContainsKey("link"));
            c = config["link"];
            Assert.IsInstanceOf<JObject>(c); // fixme - is this what we want?

            // fixme - should we resolveUrl in configs?
        }
    }
}
