using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using Xamarin.Forms.Xaml;

namespace Xamarin.Forms.StyleSheets
{
	public sealed class StyleSheet : IStyle
	{
		StyleSheet()
		{
		}

		internal IDictionary<Selector, Style> Styles { get; set; } = new Dictionary<Selector, Style>();

		//used by code generated by XamlC. Has to stay public
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static StyleSheet FromAssemblyResource(Assembly assembly, string resourceId, IXmlLineInfo lineInfo = null)
		{
			using (var stream = assembly.GetManifestResourceStream(resourceId)) {
				if (stream == null)
					throw new XamlParseException($"No resource found for '{resourceId}'.", lineInfo);
				using (var reader = new StreamReader(stream)) {
					return FromReader(reader);
				}
			}
		}
		//used by code generated by XamlC. Has to stay public
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static StyleSheet FromResource(string resourcePath, Assembly assembly, IXmlLineInfo lineInfo = null)
		{
			var styleSheet = new StyleSheet();
			var resString = DependencyService.Get<IResourcesLoader>().GetResource(resourcePath, assembly, styleSheet, lineInfo);
			Parse(styleSheet, new CssReader(new StringReader(resString)));
			return styleSheet;
		}

		//used by code generated by XamlC. Has to stay public
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static StyleSheet FromString(string stylesheet)
		{
			if (stylesheet == null)
				throw new ArgumentNullException(nameof(stylesheet));
			using (var reader = new StringReader(stylesheet))
				return FromReader(reader);
		}

		public static StyleSheet FromReader(TextReader reader)
		{
			if (reader == null)
				throw new ArgumentNullException(nameof(reader));

			var sheet = new StyleSheet();
			Parse(sheet, new CssReader(reader));
			return sheet;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static void Parse(StyleSheet sheet, CssReader reader)
		{
			Style style = null;
			var selector = Selector.All;

			int p;
			bool inStyle = false;
			reader.SkipWhiteSpaces();
			while ((p = reader.Peek()) > 0) {
				switch ((char)p) {
				case '@':
					throw new NotSupportedException("AT-rules not supported");
				case '{':
					reader.Read();
					style = Style.Parse(reader, '}');
					inStyle = true;
					break;
				case '}':
					reader.Read();
					if (!inStyle)
						throw new Exception();
					inStyle = false;
					sheet.Styles.Add(selector, style);
					style = null;
					selector = Selector.All;
					break;
				default:
					selector = Selector.Parse(reader, '{');
					break;
				}
			}
		}

		Type IStyle.TargetType
			=> typeof(VisualElement);

		void IStyle.Apply(BindableObject bindable)
		{
			var styleable = bindable as Element;
			if (styleable == null)
				return;
			Apply(styleable);
		}

		void Apply(Element styleable)
		{
			var visualStylable = styleable as VisualElement;
			if (visualStylable == null)
				return;
			foreach (var kvp in Styles) {
				var selector = kvp.Key;
				var style = kvp.Value;
				if (!selector.Matches(styleable))
					continue;
				style.Apply(visualStylable);
			}
		}

		void IStyle.UnApply(BindableObject bindable)
		{
			throw new NotImplementedException();
		}
	}
}
