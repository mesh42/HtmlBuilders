﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using HtmlAgilityPack;
using HtmlBuilders.Comparers;

namespace HtmlBuilders
{
    /// <summary>
    ///     Represents an html tag that can have a parent, children, attributes, etc.
    /// </summary>
    public class HtmlTag : HtmlElement, IDictionary<string, string>
    {
        /// <summary>
        ///     The inner list of contents
        /// </summary>
        private IList<HtmlElement> _contents = new List<HtmlElement>();

        /// <summary>
        ///     The inner <see cref="TagBuilder"/>
        /// </summary>
        private readonly TagBuilder _tagBuilder;

        /// <summary>
        ///     Initializes a new instance of <see cref="HtmlTag"/>
        /// </summary>
        /// <param name="tagName">The tag name</param>
        public HtmlTag(string tagName)
        {
            if(tagName == null)
                throw new ArgumentNullException("tagName");
            _tagBuilder = new TagBuilder(tagName);
        }

        /// <summary>
        ///     Gets the tag name
        /// </summary>
        public string TagName
        {
            get { return _tagBuilder.TagName; }
        }
        
        #region DOM Traversal

        /// <summary>
        ///     Gets the children in the order that they were added.
        ///     <br/><strong>WARNING</strong>: Text nodes (<see cref="HtmlText"/>) do not count as children and will not be included in this property.
        ///     See <see cref="Contents"/> if you want the text nodes to be included.
        /// </summary>
        public IEnumerable<HtmlTag> Children
        {
            get { return Contents.Where(c => c is HtmlTag).Cast<HtmlTag>(); }
        }

        /// <summary>
        ///     Gets or sets the contents.
        ///     This property is very similar to the <see cref="TagBuilder.InnerHtml"/> property, save for the fact that instead of just a string 
        ///     this is now a collection of elements. This allows for more extensive manipulation and DOM traversal similar to what can be done with jQuery.
        /// </summary>
        public IEnumerable<HtmlElement> Contents { get { return _contents; } set { _contents = value.ToList(); } }

        /// <summary>
        ///     Gets the (optional) parent of this <see cref="HtmlTag"/>.
        /// </summary>
        public override sealed HtmlTag Parent { get; internal set; }

        /// <summary>
        ///     Gets the parents of this <see cref="HtmlTag"/> in a 'from inside out' order.
        /// </summary>
        public IEnumerable<HtmlTag> Parents
        {
            get
            {
                return Parent == null ? Enumerable.Empty<HtmlTag>() : new[] { Parent }.Concat(Parent.Parents);
            }
        }

        /// <summary>
        ///     Gets the siblings of this <see cref="HtmlTag"/>
        /// </summary>
        public IEnumerable<HtmlTag> Siblings
        {
            get
            {
                if (Parent == null)
                    return Enumerable.Empty<HtmlTag>();
                return Parent.Children.Where(child => child != this);
            }
        }

        /// <summary>
        ///     Finds the children or the children of those children, etc. that match the <paramref name="filter"/>
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public IEnumerable<HtmlTag> Find(Func<HtmlTag, bool> filter)
        {
            return Children.Where(filter).Concat(Children.SelectMany(c => c.Find(filter)));
        }  

        /// <summary>
        ///     Prepends an <see cref="HtmlElement"/> to the <see cref="Contents"/>
        /// </summary>
        /// <param name="element">The element that will be inserted at the beginning of the contents of this tag, before all other content elements</param>
        /// <returns>this <see cref="HtmlTag"/></returns>
        public HtmlTag Prepend(HtmlElement element)
        {
            return Insert(0, element);
        }

        /// <summary>
        ///     Prepends an <see cref="HtmlText"/> to the <see cref="Contents"/>
        /// </summary>
        /// <param name="text">The text that will be inserted as a <see cref="HtmlText"/> at the beginning of the contents of this tag, before all other content elements</param>
        /// <returns>this <see cref="HtmlTag"/></returns>
        public HtmlTag Prepend(string text)
        {
            if(text == null)
                throw new ArgumentNullException("text");
            return Insert(0, new HtmlText(text));
        }

        /// <summary>
        ///     Inserts an <see cref="HtmlElement"/> to the <see cref="Contents"/> at the given <paramref name="index"/>
        /// </summary>
        /// <param name="index">The index at which the <paramref name="element"/> should be inserted</param>
        /// <param name="element">The element that will be inserted at the specifix <paramref name="index"/> of the contents of this tag</param>
        /// <returns>this <see cref="HtmlTag"/></returns>
        public HtmlTag Insert(int index, HtmlElement element)
        {
            if (element == null)
                throw new ArgumentNullException("element");
            if(index < 0 || index > _contents.Count)
                throw new IndexOutOfRangeException(string.Format("Cannot insert element '{0}' at index '{1}', content elements count = {2}", element, index, Contents.Count()));
            _contents.Insert(index, element);
            element.Parent = this;
            return this;
        }

        /// <summary>
        ///     Inserts an <see cref="HtmlElement"/> to the <see cref="Contents"/> at the given <paramref name="index"/>
        /// </summary>
        /// <param name="index">The index at which the <paramref name="text"/> should be inserted</param>
        /// <param name="text">The text that will be inserted as a <see cref="HtmlText"/> at the specifix <paramref name="index"/> of the contents of this tag</param>
        /// <returns>this <see cref="HtmlTag"/></returns>
        public HtmlTag Insert(int index, string text)
        {
            if (text == null)
                throw new ArgumentNullException("text");
            return Insert(index, new HtmlText(text));
        }

        /// <summary>
        ///     Appends an <see cref="HtmlElement"/> to the <see cref="Contents"/>
        /// </summary>
        /// <param name="element">The element that will be inserted at the end of the contents of this tag, after all other content elements</param>
        /// <returns>this <see cref="HtmlTag"/></returns>
        public HtmlTag Append(HtmlElement element)
        {
            _contents.Add(element);
            element.Parent = this;
            return this;
        }

        /// <summary>
        ///     Appends an <see cref="HtmlElement"/> to the <see cref="Contents"/>
        /// </summary>
        /// <param name="text">The text that will be inserted as a <see cref="HtmlText"/> at the end of the contents of this tag, after all other content elements</param>
        /// <returns>this <see cref="HtmlTag"/></returns>
        public HtmlTag Append(string text)
        {
            if (text == null)
                throw new ArgumentNullException("text");
            return Append(new HtmlText(text));
        }

        #endregion

        #region IDictionary<string, string> implementation for Attributes

        public int Count
        {
            get { return _tagBuilder.Attributes.Count; }
        }

        public bool IsReadOnly
        {
            get { return _tagBuilder.Attributes.IsReadOnly; }
        }

        public ICollection<string> Keys
        {
            get { return _tagBuilder.Attributes.Keys; }
        }

        public ICollection<string> Values
        {
            get { return _tagBuilder.Attributes.Values; }
        }

        public string this[string attribute]
        {
            get { return _tagBuilder.Attributes[attribute]; }
            set { _tagBuilder.Attributes[attribute] = value; }
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return _tagBuilder.Attributes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _tagBuilder.Attributes.GetEnumerator();
        }

        public void Add(KeyValuePair<string, string> item)
        {
            _tagBuilder.Attributes.Add(item);
        }

        public void Clear()
        {
            _tagBuilder.Attributes.Clear();
        }

        public bool Contains(KeyValuePair<string, string> item)
        {
            return _tagBuilder.Attributes.Contains(item);
        }

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            _tagBuilder.Attributes.CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<string, string> item)
        {
            return _tagBuilder.Attributes.Remove(item);
        }

        public bool ContainsKey(string attribute)
        {
            return _tagBuilder.Attributes.ContainsKey(attribute);
        }

        public void Add(string attribute, string value)
        {
            _tagBuilder.Attributes.Add(attribute, value);
        }

        public bool Remove(string attribute)
        {
            return _tagBuilder.Attributes.Remove(attribute);
        }

        public bool TryGetValue(string attribute, out string value)
        {
            return _tagBuilder.Attributes.TryGetValue(attribute, out value);
        }

        #endregion

        #region Attribute

        /// <summary>
        ///     Alias method for <see cref="ContainsKey"/>
        /// </summary>
        /// <param name="attribute">The attribute</param>
        /// <returns>True if the attribute was present in the attributes dictionary or false otherwise</returns>
        public bool HasAttribute(string attribute)
        {
            return ContainsKey(attribute);
        }

        /// <summary>
        ///     Sets an attribute on this tag
        /// </summary>
        /// <param name="attribute">The attribute to set</param>
        /// <param name="value">The value to set</param>
        /// <param name="replaceExisting">
        ///     A value indicating whether the <paramref name="value"/> should override the existing value for the <paramref name="attribute"/>, if any.
        /// </param>
        /// <returns>This <see cref="HtmlTag"/></returns>
        public HtmlTag Attribute(string attribute, string value, bool replaceExisting = true)
        {
            if (attribute == null)
                throw new ArgumentNullException("attribute");
            _tagBuilder.MergeAttribute(attribute, value, replaceExisting);
            return this;
        }

        #endregion

        #region Conventional attribute methods

        /// <summary>
        ///     Sets the name property. This is a shorthand for the <see cref="Attribute"/> method with 'name' as the attribute parameter value.
        /// </summary>
        /// <param name="name">The value for the 'name' attribute</param>
        ///<param name="replaceExisting">A value indicating whether the existing attribute, if any, should have its value replaced by the <paramref name="name"/> provided.</param>
        /// <returns>This <see cref="HtmlTag"/></returns>
        public HtmlTag Name(string name, bool replaceExisting = true)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            return Attribute("name", name, replaceExisting);
        }

        /// <summary>
        ///     Sets the title property. This is a shorthand for the <see cref="Attribute"/> method with 'title' as the attribute parameter value.
        /// </summary>
        /// <param name="title">The value for the 'title' attribute</param>
        ///<param name="replaceExisting">A value indicating whether the existing attribute, if any, should have its value replaced by the <paramref name="title"/> provided.</param>
        /// <returns>This <see cref="HtmlTag"/></returns>
        public HtmlTag Title(string title, bool replaceExisting = true)
        {
            if (title == null)
                throw new ArgumentNullException("title");
            return Attribute("title", title, replaceExisting);
        }

        /// <summary>
        ///     Sets the id property. This is a shorthand for the <see cref="Attribute"/> method with 'id' as the attribute parameter value.
        /// </summary>
        /// <param name="id">The value for the 'id' attribute</param>
        ///<param name="replaceExisting">A value indicating whether the existing attribute, if any, should have its value replaced by the <paramref name="id"/> provided.</param>
        /// <returns>This <see cref="HtmlTag"/></returns>
        public HtmlTag Id(string id, bool replaceExisting = true)
        {
            if (id == null)
                throw new ArgumentNullException("id");
            return Attribute("id", id, replaceExisting);
        }

        /// <summary>
        ///     Sets the type property. This is a shorthand for the <see cref="Attribute"/> method with 'type' as the attribute parameter value.
        /// </summary>
        /// <param name="type">The value for the 'type' attribute</param>
        ///<param name="replaceExisting">A value indicating whether the existing attribute, if any, should have its value replaced by the <paramref name="type"/> provided.</param>
        /// <returns>This <see cref="HtmlTag"/></returns>
        public HtmlTag Type(string type, bool replaceExisting = true)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            return Attribute("type", type, replaceExisting);
        }

        #endregion

        #region Attributes that can be toggled

        /// <summary>
        ///     Triggers an attribute on this tag. Common examples include "checked", "selected", "disabled", ...
        /// </summary>
        /// <param name="attribute">The name of the attribute</param>
        /// <param name="value">A value indicating whether this attribute should be set on this tag or not.</param>
        /// <returns>This <see cref="HtmlTag"/></returns>
        public HtmlTag ToggleAttribute(string attribute, bool value)
        {
            if(attribute == null)
                throw new ArgumentNullException("attribute");
            if (value)
                return Attribute(attribute, attribute);
            Remove(attribute);
            return this;
        }

        /// <summary>
        ///     Sets the 'checked' attribute to 'checked' if <paramref name="checked"/> is true or removes the attribute if <paramref name="checked"/> is false
        /// </summary>
        /// <param name="checked">A value indicating whether this tag should have the attribute 'checked' with value 'checked' or not.</param>
        /// <returns>This <see cref="HtmlTag"/></returns>
        public HtmlTag Checked(bool @checked)
        {
            return ToggleAttribute("checked", @checked);
        }

        /// <summary>
        ///     Sets the 'disabled' attribute to 'disabled' if <paramref name="disabled"/> is true or removes the attribute if <paramref name="disabled"/> is false
        /// </summary>
        /// <param name="disabled">A value indicating whether this tag should have the attribute 'disabled' with value 'disabled' or not.</param>
        /// <returns>This <see cref="HtmlTag"/></returns>
        public HtmlTag Disabled(bool disabled)
        {
            return ToggleAttribute("disabled", disabled);
        }

        /// <summary>
        ///     Sets the 'selected' attribute to 'selected' if <paramref name="selected"/> is true or removes the attribute if <paramref name="selected"/> is false
        /// </summary>
        /// <param name="selected">A value indicating whether this tag should have the attribute 'selected' with value 'selected' or not.</param>
        /// <returns>This <see cref="HtmlTag"/></returns>
        public HtmlTag Selected(bool selected)
        {
            return ToggleAttribute("selected", selected);
        }

        #endregion

        #region Data Attributes

        /// <summary>
        ///     Sets a data attribute. This method will automatically prepend 'data-' to the attribute name if the attribute name does not start with 'data-'.
        /// </summary>
        /// <param name="attribute">The name of the attribute</param>
        /// <param name="value">The value</param>
        /// <param name="replaceExisting">A value indicating whether the existing data attribute, if any, should have its value replace by the <paramref name="value"/> provided.</param>
        /// <returns>This <see cref="HtmlTag"/></returns>
        public HtmlTag Data(string attribute, string value, bool replaceExisting = true)
        {
            if(attribute == null)
                throw new ArgumentNullException("attribute");
            return Attribute(attribute.StartsWith("data-") ? attribute : "data-" + attribute, value, replaceExisting);
        }

        /// <summary>
        ///     Sets a data attribute. This method will automatically prepend 'data-' to the attribute name if the attribute name does not start with 'data-'.
        /// </summary>
        /// <param name="data">The anonymous data object containing properties that should be set as data attributes</param>
        /// <param name="replaceExisting">A value indicating whether the existing data attributes, if any, should have their values replaced by the values found in <paramref name="data"/></param>
        /// <example>
        /// <code>
        ///     // results in &lt;a data-index-url="/index" data-about-url="/about"&gt;&lt;a&gt;
        ///     new HtmlTag('a').Data(new { index_url = "/index", about_url = "/about"}).ToHtml() 
        /// </code>
        /// </example>
        /// <returns>This <see cref="HtmlTag"/></returns>
        public HtmlTag Data(object data, bool replaceExisting = true)
        {
            if(data == null)
                throw new ArgumentNullException("data");
            var htmlAttributes = HtmlHelper.AnonymousObjectToHtmlAttributes(data);
            foreach (var htmlAttribute in htmlAttributes)
            {
                string attribute = htmlAttribute.Key;
                Attribute(attribute.StartsWith("data-") ? attribute : "data-" + attribute, Convert.ToString(htmlAttribute.Value), replaceExisting);
            }
            return this;
        }

        #endregion

        #region Styles

        /// <summary>
        ///     Gets or sets the 'style' attribute of this <see cref="HtmlTag"/>.
        ///     Note that this is a utility method that parses the 'style' attribute from a string into a <see cref="IReadOnlyDictionary{TKey,TValue}"/>
        /// </summary>
        public IReadOnlyDictionary<string, string> Styles
        {
            get
            {
                string styles;
                if (!TryGetValue("style", out styles))
                    return new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
                var styleRules = styles.Split(';').Select(styleRule => styleRule.Split(':')).ToArray();
                if (styleRules.All(s => s.Length == 2))
                    return styleRules.ToDictionary(styleRule => styleRule[0], styleRule => styleRule[1]);
                var invalidStyleRules = styleRules.Where(s => s.Length != 2);
                throw new InvalidOperationException(string.Format("Detected invalid style rules: {0}", string.Join(",", invalidStyleRules.Select(s => string.Join(":", s)))));
            }
            set
            {
                if (value.Count == 0)
                {
                    Remove("style");
                }
                else
                {
                    string newStyle = string.Join(";", value.Select(s => string.Format("{0}:{1}", s.Key, s.Value)));
                    Attribute("style", newStyle);
                }
            }
        }

        /// <summary>
        ///     Sets a css style <paramref name="key"/> and <paramref name="value"/> on the 'style' attribute.
        /// </summary>
        /// <param name="key">The type of the style (width, height, margin, padding, ...)</param>
        /// <param name="value">The value of the style (any valid css value for the given <paramref name="key"/>)</param>
        /// <param name="replaceExisting">A value indicating whether the existing value for the given <paramref name="key"/> should be updated or not, if such a key is already present in the 'style' attribute.</param>
        /// <returns></returns>
        public HtmlTag Style(string key, string value, bool replaceExisting = true)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            if (value == null)
                throw new ArgumentNullException("value");
            if (key.Contains(";"))
                throw new ArgumentException(string.Format("Style key cannot contain ';'! Key was '{0}'", key));
            if (value.Contains(";"))
                throw new ArgumentException(string.Format("Style value cannot contain ';'! Value was '{0}'", key));
            var styles = Styles.ToDictionary(s => s.Key, s => s.Value);
            if (!styles.ContainsKey(key) || replaceExisting)
                styles[key] = value;
            Styles = styles;
            return this;
        }

        /// <summary>
        ///     Removes a <paramref name="key"/> from the <see cref="Styles"/>, if such a key is present.
        /// </summary>
        /// <param name="key">The key to remove from the style</param>
        /// <returns></returns>
        public HtmlTag RemoveStyle(string key)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            Styles = Styles.Where(s => !string.Equals(s.Key, key)).ToDictionary(s => s.Key, s => s.Value);
            return this;
        }

        /// <summary>
        ///     Sets the width style. This is a shorthand for calling the <see cref="Style"/> method with the 'width' key
        /// </summary>
        /// <param name="width">The width. This can be any valid css value for 'width'</param>
        /// <param name="replaceExisting">A value indicating whether the existing width, if any, should be overriden or not</param>
        /// <returns>This <see cref="HtmlTag"/></returns>
        public HtmlTag Width(string width, bool replaceExisting = true)
        {
            if (width == null)
                throw new ArgumentNullException("width");
            return Style("width", width, replaceExisting);
        }

        /// <summary>
        ///     Sets the height style. This is a shorthand for calling the <see cref="Style"/> method with the 'height' key
        /// </summary>
        /// <param name="height">The height. This can be any valid css value for 'height'</param>
        /// <param name="replaceExisting">A value indicating whether the existing height, if any, should be overriden or not</param>
        /// <returns>This <see cref="HtmlTag"/></returns>
        public HtmlTag Height(string height, bool replaceExisting = true)
        {
            if (height == null)
                throw new ArgumentNullException("height");
            return Style("height", height, replaceExisting);
        }

        #endregion

        #region Class

        /// <summary>
        ///     Gets or sets the classes of this <see cref="HtmlTag"/>
        ///     This is a utility method to easily manipulate the 'class' attribute
        /// </summary>
        public IEnumerable<string> Classes
        {
            get
            {
                string classes;
                return TryGetValue("class", out classes) ? classes.Split(' ') : Enumerable.Empty<string>();
            }
            set
            {
                if (!value.Any())
                {
                    Remove("class");
                }
                else
                {
                    Attribute("class", string.Join(" ", value));
                }
                
            }
        }

        /// <summary>
        ///     Returns true if this <see cref="HtmlTag"/> has the <paramref name="class"/> or false otherwise
        /// </summary>
        /// <param name="class">The class</param>
        /// <returns>True if this <see cref="HtmlTag"/> has the <paramref name="class"/> or false otherwise</returns>
        public bool HasClass(string @class)
        {
            return Classes.Any(c => string.Equals(c, @class));
        }

        /// <summary>
        ///     Adds a class to this tag.
        /// </summary>
        /// <param name="class">The class(es) to add</param>
        /// <returns>This <see cref="HtmlTag"/></returns>
        public HtmlTag Class(string @class)
        {
            if(@class == null)
                throw new ArgumentNullException("class");
            var classesToAdd = @class.Split(' ');
            Classes = Classes.Concat(classesToAdd).Distinct();
            return this;
        }

        /// <summary>
        ///     Removes one or more classes from this tag.
        /// </summary>
        /// <param name="class">The class(es) to remove</param>
        /// <returns></returns>
        public HtmlTag RemoveClass(string @class)
        {
            if(@class == null)
                throw new ArgumentNullException("class");
            var classesToRemove = @class.Split(' ');
            Classes = Classes.Where(c => !classesToRemove.Contains(c));
            return this;
        }

        #endregion

        #region Merge attributes by dictionary or anonymous object

        /// <summary>
        ///     Adds new attributes or optionally replaces existing attributes in the tag.
        /// </summary>
        /// <param name="attributes">The collection of attributes to add or replace.</param>
        /// <typeparam name="TKey">The type of the key object.</typeparam>
        /// <typeparam name="TValue">The type of the value object.</typeparam>
        public HtmlTag Merge<TKey, TValue>(IDictionary<TKey, TValue> attributes)
        {
            _tagBuilder.MergeAttributes(attributes);
            return this;
        }

        /// <summary>
        ///     Adds new attributes or optionally replaces existing attributes in the tag.
        /// </summary>
        /// <param name="attributes">The collection of attributes to add or replace.</param>
        /// <param name="replaceExisting">
        ///     For each attribute in <paramref name="attributes" />, true to replace the attribute if an
        ///     attribute already exists that has the same key, or false to leave the original attribute unchanged.
        /// </param>
        /// <typeparam name="TKey">The type of the key object.</typeparam>
        /// <typeparam name="TValue">The type of the value object.</typeparam>
        public HtmlTag Merge<TKey, TValue>(IDictionary<TKey, TValue> attributes, bool replaceExisting)
        {
            _tagBuilder.MergeAttributes(attributes, replaceExisting);
            return this;
        }

        /// <summary>
        ///     Adds new attributes or optionally replaces existing attributes in the tag.
        /// </summary>
        /// <param name="attributes">The collection of attributes to add or replace.</param>
        public HtmlTag Merge(object attributes)
        {
            return Merge(HtmlHelper.AnonymousObjectToHtmlAttributes(attributes));
        }

        /// <summary>
        ///     Adds new attributes or optionally replaces existing attributes in the tag.
        /// </summary>
        /// <param name="attributes">The collection of attributes to add or replace.</param>
        /// <param name="replaceExisting">
        ///     For each attribute in <paramref name="attributes" />, true to replace the attribute if an
        ///     attribute already exists that has the same key, or false to leave the original attribute unchanged.
        /// </param>
        public HtmlTag Merge(object attributes, bool replaceExisting)
        {
            return Merge(HtmlHelper.AnonymousObjectToHtmlAttributes(attributes), replaceExisting);
        }

        #endregion

        #region To Html
        
        /// <summary>
        ///     Renders and returns the HTML tag by using the specified render mode.
        /// </summary>
        /// <param name="tagRenderMode">
        ///     The render mode. 
        ///     <br/><strong>IMPORTANT: </strong> When using <see cref="TagRenderMode.StartTag"/> or <see cref="TagRenderMode.EndTag"/>, 
        ///     the <see cref="Contents"/> of this <see cref="HtmlTag"/> will not be rendered.
        ///     This is because when you have more than 1 content element, it does not make sense to only render the start or end tags. Since the API exposes the
        ///     <see cref="Contents"/> and <see cref="Children"/> separately, the responsibility is with the user of this class to render the HTML as he wishes.
        ///     However, when using <see cref="TagRenderMode.Normal"/> (or passing no parameters, since <see cref="TagRenderMode.Normal"/> is the default value),
        ///     the <see cref="Contents"/> <strong>will</strong> be taken into account since it is what you would expect.
        /// </param>
        /// <returns>The rendered HTML tag by using the specified render mode</returns>
        /// <exception cref="InvalidOperationException">When <see cref="TagRenderMode.SelfClosing"/> is used but the <see cref="HtmlTag"/> is not empty. (The <see cref="Contents"/> are not empty)</exception>
        public override sealed IHtmlString ToHtml(TagRenderMode tagRenderMode = TagRenderMode.Normal)
        {
            var stringBuilder = new StringBuilder();
            switch (tagRenderMode)
            {
                case TagRenderMode.StartTag:
                    stringBuilder.Append(_tagBuilder.ToString(TagRenderMode.StartTag));
                    break;
                case TagRenderMode.EndTag:
                    stringBuilder.Append(_tagBuilder.ToString(TagRenderMode.EndTag));
                    break;
                case TagRenderMode.SelfClosing:
                    if (Contents.Any())
                    {
                        throw new InvalidOperationException("Cannot render this tag with the self closing TagRenderMode because this tag has inner contents: Count = " + Contents.Count());
                    }
                    stringBuilder.Append(_tagBuilder.ToString(TagRenderMode.SelfClosing));
                    break;
                default:
                    stringBuilder.Append(_tagBuilder.ToString(TagRenderMode.StartTag));
                    foreach (var content in Contents)
                    {
                        stringBuilder.Append(content.ToHtml());
                    }
                    stringBuilder.Append(_tagBuilder.ToString(TagRenderMode.EndTag));
                    break;
            }
            return MvcHtmlString.Create(stringBuilder.ToString());
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return ToHtml().ToHtmlString();
        }

        #endregion 

        #region Factory methods

        /// <summary>
        ///     Parses an <see cref="HtmlTag"/> from the given <paramref name="html"/>
        /// </summary>
        /// <param name="html">The html</param>
        /// <returns>A new <see cref="HtmlTag"/> that is an object representation of the <paramref name="html"/></returns>
        public static HtmlTag Parse(string html)
        {
            if (html == null)
                throw new ArgumentNullException("html");
            return Parse(new StringReader(html));
        }


        /// <summary>
        ///     Parses an <see cref="HtmlTag"/> from the given <paramref name="textReader"/>
        /// </summary>
        /// <param name="textReader">The text reader</param>
        /// <returns>A new <see cref="HtmlTag"/> that is an object representation of the <paramref name="textReader"/></returns>
        public static HtmlTag Parse(TextReader textReader)
        {
            if (textReader == null)
                throw new ArgumentNullException("textReader");
            var htmlDocument = new HtmlDocument();
            htmlDocument.Load(textReader);
            return Parse(htmlDocument);
        }

        /// <summary>
        ///     Parses an <see cref="HtmlTag"/> from the given <paramref name="htmlDocument"/>
        /// </summary>
        /// <param name="htmlDocument">The html document containing the html</param>
        /// <returns>A new <see cref="HtmlTag"/> that is an object representation of the <paramref name="htmlDocument"/></returns>
        public static HtmlTag Parse(HtmlDocument htmlDocument)
        {
            if (htmlDocument.ParseErrors.Any())
            {
                var readableErrors = htmlDocument.ParseErrors.Select(e => string.Format("Code = {0}, SourceText = {1}, Reason = {2}", e.Code, e.SourceText, e.Reason));
                throw new InvalidOperationException(string.Format("Parse errors found: \n{0}", string.Join("\n", readableErrors)));
            }
            if(htmlDocument.DocumentNode.ChildNodes.Count != 1)
                throw new ArgumentException("Html contains more than one element. The parse method can only be used for single html tags! Input was : " + htmlDocument.DocumentNode);

            return ParseHtmlTag(htmlDocument.DocumentNode.ChildNodes.Single());
        }

        private static HtmlTag ParseHtmlTag(HtmlNode htmlNode)
        {
            var htmlTag = new HtmlTag(htmlNode.Name);
            foreach (var attribute in htmlNode.Attributes)
            {
                htmlTag.Attribute(attribute.Name, attribute.Value);
            }
            foreach (var childNode in htmlNode.ChildNodes)
            {
                HtmlElement childElement = null;
                switch (childNode.NodeType)
                {
                    case HtmlNodeType.Element:
                        childElement = ParseHtmlTag(childNode);
                        break;
                    case HtmlNodeType.Text:
                        childElement = ParseHtmlText(childNode);
                        break;
                }
                if (childElement != null)
                    htmlTag.Append(childElement);
            }
            return htmlTag;
        }

        private static HtmlText ParseHtmlText(HtmlNode htmlNode)
        {
            return new HtmlText(htmlNode.InnerText);
        }

        #endregion

        #region Equality
        // compare parent, tag name & attributes
        private bool Equals(HtmlTag other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            if (!string.Equals(TagName, other.TagName))
                return false;
            if (Count != other.Count)
                return false;
            if (!DictionaryComparer.Equals(this, other, keysToExclude: new[] { "class", "style" }))
                return false;
            if (!DictionaryComparer.Equals(Styles, other.Styles))
                return false;
            return Classes.OrderBy(c => c).SequenceEqual(other.Classes.OrderBy(c => c))
                && Contents.SequenceEqual(other.Contents);
        }

        public override bool Equals(object other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return other.GetType() == GetType() && Equals((HtmlTag)other);
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + TagName.GetHashCode();
            foreach (var attribute in this.Where(attribute => !string.Equals(attribute.Key, "style") && !string.Equals(attribute.Key, "class"))
                                          .OrderBy(attribute => attribute.Key))
            {
                hash = hash * 23 + attribute.Key.GetHashCode();
                hash = hash * 23 + attribute.Value.GetHashCode();
            }
            foreach (var style in Styles.OrderBy(style => style.Key))
            {
                hash = hash * 23 + style.Key.GetHashCode();
                hash = hash * 23 + style.Value.GetHashCode();
            }
            return Classes.OrderBy(c => c).Aggregate(hash, (current, @class) => current * 23 + @class.GetHashCode());
        }

        #endregion
        
    }
}