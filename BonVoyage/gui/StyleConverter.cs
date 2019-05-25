using UnityEngine;

namespace UI
{
    /// <summary>
    /// Convert GUI skin and style to UI skin and style
    /// </summary>
    internal static class StyleConverter
    {
        /// <summary>
        /// Convert GUI skin to UI skin
        /// </summary>
        /// <param name="skin"></param>
        /// <returns></returns>
        internal static UISkinDef Convert(GUISkin skin)
        {
            UISkinDef def = new UISkinDef();
            if (skin != null)
            {
                def.box = ConvertStyle(skin.box);
                def.button = ConvertStyle(skin.button);
                def.font = skin.font;
                def.horizontalScrollbar = ConvertStyle(skin.horizontalScrollbar);
                def.horizontalScrollbarLeftButton = ConvertStyle(skin.horizontalScrollbarLeftButton);
                def.horizontalScrollbarRightButton = ConvertStyle(skin.horizontalScrollbarRightButton);
                def.horizontalScrollbarThumb = ConvertStyle(skin.horizontalScrollbarThumb);
                def.horizontalSlider = ConvertStyle(skin.horizontalSlider);
                def.horizontalSliderThumb = ConvertStyle(skin.horizontalSliderThumb);
                def.label = ConvertStyle(skin.label);
                def.name = skin.name;
                def.scrollView = ConvertStyle(skin.scrollView);
                def.textArea = ConvertStyle(skin.textArea);
                def.textField = ConvertStyle(skin.textField);
                def.toggle = ConvertStyle(skin.toggle);
                def.verticalScrollbar = ConvertStyle(skin.verticalScrollbar);
                def.verticalScrollbarDownButton = ConvertStyle(skin.verticalScrollbarDownButton);
                def.verticalScrollbarThumb = ConvertStyle(skin.verticalScrollbarThumb);
                def.verticalScrollbarUpButton = ConvertStyle(skin.verticalScrollbarUpButton);
                def.verticalSlider = ConvertStyle(skin.verticalSlider);
                def.verticalSliderThumb = ConvertStyle(skin.verticalSliderThumb);
                def.window = ConvertStyle(skin.window);
            }

            return def;
        }


        /// <summary>
        /// Convert GUI style to UI style
        /// </summary>
        /// <param name="guiStyle"></param>
        /// <returns></returns>
        private static UIStyle ConvertStyle(GUIStyle guiStyle)
        {
            UIStyle style = new UIStyle();
            if (guiStyle != null)
            {
                style.active = ConvertStyleState(guiStyle.active);
                style.alignment = guiStyle.alignment;
                style.disabled = ConvertStyleState(guiStyle.active);
                style.clipping = guiStyle.clipping;
                style.fixedHeight = guiStyle.fixedHeight;
                style.fixedWidth = guiStyle.fixedWidth;
                style.font = guiStyle.font;
                style.fontSize = guiStyle.fontSize;
                style.fontStyle = guiStyle.fontStyle;
                style.highlight = ConvertStyleState(guiStyle.focused);
                style.lineHeight = guiStyle.lineHeight;
                style.name = guiStyle.name;
                style.normal = ConvertStyleState(guiStyle.normal);
                style.richText = guiStyle.richText;
                style.stretchHeight = guiStyle.stretchHeight;
                style.stretchWidth = guiStyle.stretchWidth;
                style.wordWrap = guiStyle.wordWrap;
            }

            return style;
        }


        /// <summary>
        /// Convert GUI style state to UI style state
        /// </summary>
        /// <param name="guiStyleState"></param>
        /// <returns></returns>
        private static UIStyleState ConvertStyleState(GUIStyleState guiStyleState)
        {
            UIStyleState state = new UIStyleState();
            if (guiStyleState != null)
            {
                if (guiStyleState.background != null)
                {
                    state.background = Sprite.Create(guiStyleState.background,
                        new Rect(0, 0, guiStyleState.background.width, guiStyleState.background.height),
                        new Vector2(0.5f, 0.5f), 100, 1, SpriteMeshType.Tight, Vector4.one * 5);
                }

                state.textColor = guiStyleState.textColor;
            }

            return state;
        }

    }

}