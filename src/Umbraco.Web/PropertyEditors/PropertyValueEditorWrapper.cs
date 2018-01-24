﻿using Umbraco.Core.PropertyEditors;

namespace Umbraco.Web.PropertyEditors
{
    /// <summary>
    /// Useful when returning a custom value editor when your property editor is attributed, it ensures the attribute
    /// values are copied across to your custom value editor.
    /// </summary>
#error wtf
    public class PropertyValueEditorWrapper : ValueEditor // fixme but WHAT is this exactly?!
    {
        public PropertyValueEditorWrapper(ValueEditor wrapped)
        {
            this.HideLabel = wrapped.HideLabel;
            this.View = wrapped.View;
            this.ValueType = wrapped.ValueType;
            foreach (var v in wrapped.Validators)
            {
                Validators.Add(v);
            }
        }
    }
}
