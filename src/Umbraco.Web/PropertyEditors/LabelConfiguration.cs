﻿using Umbraco.Core;
using Umbraco.Core.PropertyEditors;

namespace Umbraco.Web.PropertyEditors
{
    /// <summary>
    /// Represents the configuration for the label value editor.
    /// </summary>
    public class LabelConfiguration : IConfigureValueType
    {
        [ConfigurationField(Constants.PropertyEditors.ConfigurationKeys.DataValueType, "Value type", "valuetype")]
        public string ValueType { get; set; } = ValueTypes.String;
    }
}