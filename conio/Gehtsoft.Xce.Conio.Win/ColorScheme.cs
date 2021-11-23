
//------------------------------------------------------------------------
//This is auto-generated code. Do NOT modify it.
//Modify ./auto/colorscheme.xml and ./auto/colorscheme.xslt instead!!!
//------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using CanvasColor = Gehtsoft.Xce.Conio.CanvasColor;

namespace Gehtsoft.Xce.Conio.Win
{
    public interface IColorScheme
    {
        
        #region Window colors
        ///<summary>
        ///
        ///</summary>
        CanvasColor WindowBackground
        {
            get;
        }
        
        #endregion
        
        #region Menu colors
        ///<summary>
        ///
        ///</summary>
        CanvasColor MenuItem
        {
            get;
        }
        
        ///<summary>
        ///
        ///</summary>
        CanvasColor MenuItemSelected
        {
            get;
        }
        
        ///<summary>
        ///
        ///</summary>
        CanvasColor MenuDisabledItem
        {
            get;
        }
        
        ///<summary>
        ///
        ///</summary>
        CanvasColor MenuDisabledItemSelected
        {
            get;
        }
        
        ///<summary>
        ///
        ///</summary>
        CanvasColor MenuHotKey
        {
            get;
        }
        
        ///<summary>
        ///
        ///</summary>
        CanvasColor MenuHotKeySelected
        {
            get;
        }
        
        #endregion
        
        #region Dialog box colors
        ///<summary>
        ///
        ///</summary>
        CanvasColor DialogBorder
        {
            get;
        }
        
        #endregion
        
        #region Dialog box label colors
        ///<summary>
        ///
        ///</summary>
        CanvasColor DialogItemLabelColor
        {
            get;
        }
        
        ///<summary>
        ///
        ///</summary>
        CanvasColor DialogItemLabelHotKey
        {
            get;
        }
        
        #endregion
        
        #region Dialog box check box label colors
        ///<summary>
        ///
        ///</summary>
        CanvasColor DialogItemCheckBoxColor
        {
            get;
        }
        
        ///<summary>
        ///
        ///</summary>
        CanvasColor DialogItemCheckBoxColorFocused
        {
            get;
        }
        
        ///<summary>
        ///
        ///</summary>
        CanvasColor DialogItemCheckBoxHotKey
        {
            get;
        }
        
        ///<summary>
        ///
        ///</summary>
        CanvasColor DialogItemCheckBoxHotKeyFocused
        {
            get;
        }
        
        ///<summary>
        ///
        ///</summary>
        CanvasColor DialogItemCheckBoxColorDisabled
        {
            get;
        }
        
        #endregion
        
        #region Dialog box button colors
        ///<summary>
        ///
        ///</summary>
        CanvasColor DialogItemButtonColor
        {
            get;
        }
        
        ///<summary>
        ///
        ///</summary>
        CanvasColor DialogItemButtonColorDisabled
        {
            get;
        }
        
        ///<summary>
        ///
        ///</summary>
        CanvasColor DialogItemButtonColorFocused
        {
            get;
        }
        
        ///<summary>
        ///
        ///</summary>
        CanvasColor DialogItemButtonHotKey
        {
            get;
        }
        
        ///<summary>
        ///
        ///</summary>
        CanvasColor DialogItemButtonHotKeyFocused
        {
            get;
        }
        
        #endregion
        
        #region Dialog box edit color
        ///<summary>
        ///
        ///</summary>
        CanvasColor DialogItemEditColor
        {
            get;
        }
        
        ///<summary>
        ///
        ///</summary>
        CanvasColor DialogItemEditColorDisabled
        {
            get;
        }
        
        ///<summary>
        ///
        ///</summary>
        CanvasColor DialogItemEditColorFocused
        {
            get;
        }
        
        ///<summary>
        ///
        ///</summary>
        CanvasColor DialogItemEditSelection
        {
            get;
        }
        
        ///<summary>
        ///
        ///</summary>
        CanvasColor DialogItemEditSelectionFocused
        {
            get;
        }
        
        #endregion
        
        #region Dialog box listbox color
        ///<summary>
        ///
        ///</summary>
        CanvasColor DialogItemListBoxColor
        {
            get;
        }
        
        ///<summary>
        ///
        ///</summary>
        CanvasColor DialogItemListBoxColorDisabled
        {
            get;
        }
        
        ///<summary>
        ///
        ///</summary>
        CanvasColor DialogItemListBoxColorFocused
        {
            get;
        }
        
        ///<summary>
        ///
        ///</summary>
        CanvasColor DialogItemListBoxSelection
        {
            get;
        }
        
        ///<summary>
        ///
        ///</summary>
        CanvasColor DialogItemListBoxSelectionFocused
        {
            get;
        }
        
        ///<summary>
        ///
        ///</summary>
        CanvasColor DialogItemListBoxSelectionHighlighted
        {
            get;
        }
        
        ///<summary>
        ///
        ///</summary>
        CanvasColor DialogItemListBoxSelectionFocusedHighlighted
        {
            get;
        }
        
        #endregion
        
    }

    public class ColorScheme : IColorScheme
    {
        
        #region Window colors
        private CanvasColor mWindowBackground;
        ///<summary>
        ///
        ///</summary>
        public CanvasColor WindowBackground
        {
            get
            {
                return mWindowBackground;
            }
            set
            {
                mWindowBackground = value;
            }
        }
        
        #endregion
        
        #region Menu colors
        private CanvasColor mMenuItem;
        ///<summary>
        ///
        ///</summary>
        public CanvasColor MenuItem
        {
            get
            {
                return mMenuItem;
            }
            set
            {
                mMenuItem = value;
            }
        }
        
        private CanvasColor mMenuItemSelected;
        ///<summary>
        ///
        ///</summary>
        public CanvasColor MenuItemSelected
        {
            get
            {
                return mMenuItemSelected;
            }
            set
            {
                mMenuItemSelected = value;
            }
        }
        
        private CanvasColor mMenuDisabledItem;
        ///<summary>
        ///
        ///</summary>
        public CanvasColor MenuDisabledItem
        {
            get
            {
                return mMenuDisabledItem;
            }
            set
            {
                mMenuDisabledItem = value;
            }
        }
        
        private CanvasColor mMenuDisabledItemSelected;
        ///<summary>
        ///
        ///</summary>
        public CanvasColor MenuDisabledItemSelected
        {
            get
            {
                return mMenuDisabledItemSelected;
            }
            set
            {
                mMenuDisabledItemSelected = value;
            }
        }
        
        private CanvasColor mMenuHotKey;
        ///<summary>
        ///
        ///</summary>
        public CanvasColor MenuHotKey
        {
            get
            {
                return mMenuHotKey;
            }
            set
            {
                mMenuHotKey = value;
            }
        }
        
        private CanvasColor mMenuHotKeySelected;
        ///<summary>
        ///
        ///</summary>
        public CanvasColor MenuHotKeySelected
        {
            get
            {
                return mMenuHotKeySelected;
            }
            set
            {
                mMenuHotKeySelected = value;
            }
        }
        
        #endregion
        
        #region Dialog box colors
        private CanvasColor mDialogBorder;
        ///<summary>
        ///
        ///</summary>
        public CanvasColor DialogBorder
        {
            get
            {
                return mDialogBorder;
            }
            set
            {
                mDialogBorder = value;
            }
        }
        
        #endregion
        
        #region Dialog box label colors
        private CanvasColor mDialogItemLabelColor;
        ///<summary>
        ///
        ///</summary>
        public CanvasColor DialogItemLabelColor
        {
            get
            {
                return mDialogItemLabelColor;
            }
            set
            {
                mDialogItemLabelColor = value;
            }
        }
        
        private CanvasColor mDialogItemLabelHotKey;
        ///<summary>
        ///
        ///</summary>
        public CanvasColor DialogItemLabelHotKey
        {
            get
            {
                return mDialogItemLabelHotKey;
            }
            set
            {
                mDialogItemLabelHotKey = value;
            }
        }
        
        #endregion
        
        #region Dialog box check box label colors
        private CanvasColor mDialogItemCheckBoxColor;
        ///<summary>
        ///
        ///</summary>
        public CanvasColor DialogItemCheckBoxColor
        {
            get
            {
                return mDialogItemCheckBoxColor;
            }
            set
            {
                mDialogItemCheckBoxColor = value;
            }
        }
        
        private CanvasColor mDialogItemCheckBoxColorFocused;
        ///<summary>
        ///
        ///</summary>
        public CanvasColor DialogItemCheckBoxColorFocused
        {
            get
            {
                return mDialogItemCheckBoxColorFocused;
            }
            set
            {
                mDialogItemCheckBoxColorFocused = value;
            }
        }
        
        private CanvasColor mDialogItemCheckBoxHotKey;
        ///<summary>
        ///
        ///</summary>
        public CanvasColor DialogItemCheckBoxHotKey
        {
            get
            {
                return mDialogItemCheckBoxHotKey;
            }
            set
            {
                mDialogItemCheckBoxHotKey = value;
            }
        }
        
        private CanvasColor mDialogItemCheckBoxHotKeyFocused;
        ///<summary>
        ///
        ///</summary>
        public CanvasColor DialogItemCheckBoxHotKeyFocused
        {
            get
            {
                return mDialogItemCheckBoxHotKeyFocused;
            }
            set
            {
                mDialogItemCheckBoxHotKeyFocused = value;
            }
        }
        
        private CanvasColor mDialogItemCheckBoxColorDisabled;
        ///<summary>
        ///
        ///</summary>
        public CanvasColor DialogItemCheckBoxColorDisabled
        {
            get
            {
                return mDialogItemCheckBoxColorDisabled;
            }
            set
            {
                mDialogItemCheckBoxColorDisabled = value;
            }
        }
        
        #endregion
        
        #region Dialog box button colors
        private CanvasColor mDialogItemButtonColor;
        ///<summary>
        ///
        ///</summary>
        public CanvasColor DialogItemButtonColor
        {
            get
            {
                return mDialogItemButtonColor;
            }
            set
            {
                mDialogItemButtonColor = value;
            }
        }
        
        private CanvasColor mDialogItemButtonColorDisabled;
        ///<summary>
        ///
        ///</summary>
        public CanvasColor DialogItemButtonColorDisabled
        {
            get
            {
                return mDialogItemButtonColorDisabled;
            }
            set
            {
                mDialogItemButtonColorDisabled = value;
            }
        }
        
        private CanvasColor mDialogItemButtonColorFocused;
        ///<summary>
        ///
        ///</summary>
        public CanvasColor DialogItemButtonColorFocused
        {
            get
            {
                return mDialogItemButtonColorFocused;
            }
            set
            {
                mDialogItemButtonColorFocused = value;
            }
        }
        
        private CanvasColor mDialogItemButtonHotKey;
        ///<summary>
        ///
        ///</summary>
        public CanvasColor DialogItemButtonHotKey
        {
            get
            {
                return mDialogItemButtonHotKey;
            }
            set
            {
                mDialogItemButtonHotKey = value;
            }
        }
        
        private CanvasColor mDialogItemButtonHotKeyFocused;
        ///<summary>
        ///
        ///</summary>
        public CanvasColor DialogItemButtonHotKeyFocused
        {
            get
            {
                return mDialogItemButtonHotKeyFocused;
            }
            set
            {
                mDialogItemButtonHotKeyFocused = value;
            }
        }
        
        #endregion
        
        #region Dialog box edit color
        private CanvasColor mDialogItemEditColor;
        ///<summary>
        ///
        ///</summary>
        public CanvasColor DialogItemEditColor
        {
            get
            {
                return mDialogItemEditColor;
            }
            set
            {
                mDialogItemEditColor = value;
            }
        }
        
        private CanvasColor mDialogItemEditColorDisabled;
        ///<summary>
        ///
        ///</summary>
        public CanvasColor DialogItemEditColorDisabled
        {
            get
            {
                return mDialogItemEditColorDisabled;
            }
            set
            {
                mDialogItemEditColorDisabled = value;
            }
        }
        
        private CanvasColor mDialogItemEditColorFocused;
        ///<summary>
        ///
        ///</summary>
        public CanvasColor DialogItemEditColorFocused
        {
            get
            {
                return mDialogItemEditColorFocused;
            }
            set
            {
                mDialogItemEditColorFocused = value;
            }
        }
        
        private CanvasColor mDialogItemEditSelection;
        ///<summary>
        ///
        ///</summary>
        public CanvasColor DialogItemEditSelection
        {
            get
            {
                return mDialogItemEditSelection;
            }
            set
            {
                mDialogItemEditSelection = value;
            }
        }
        
        private CanvasColor mDialogItemEditSelectionFocused;
        ///<summary>
        ///
        ///</summary>
        public CanvasColor DialogItemEditSelectionFocused
        {
            get
            {
                return mDialogItemEditSelectionFocused;
            }
            set
            {
                mDialogItemEditSelectionFocused = value;
            }
        }
        
        #endregion
        
        #region Dialog box listbox color
        private CanvasColor mDialogItemListBoxColor;
        ///<summary>
        ///
        ///</summary>
        public CanvasColor DialogItemListBoxColor
        {
            get
            {
                return mDialogItemListBoxColor;
            }
            set
            {
                mDialogItemListBoxColor = value;
            }
        }
        
        private CanvasColor mDialogItemListBoxColorDisabled;
        ///<summary>
        ///
        ///</summary>
        public CanvasColor DialogItemListBoxColorDisabled
        {
            get
            {
                return mDialogItemListBoxColorDisabled;
            }
            set
            {
                mDialogItemListBoxColorDisabled = value;
            }
        }
        
        private CanvasColor mDialogItemListBoxColorFocused;
        ///<summary>
        ///
        ///</summary>
        public CanvasColor DialogItemListBoxColorFocused
        {
            get
            {
                return mDialogItemListBoxColorFocused;
            }
            set
            {
                mDialogItemListBoxColorFocused = value;
            }
        }
        
        private CanvasColor mDialogItemListBoxSelection;
        ///<summary>
        ///
        ///</summary>
        public CanvasColor DialogItemListBoxSelection
        {
            get
            {
                return mDialogItemListBoxSelection;
            }
            set
            {
                mDialogItemListBoxSelection = value;
            }
        }
        
        private CanvasColor mDialogItemListBoxSelectionFocused;
        ///<summary>
        ///
        ///</summary>
        public CanvasColor DialogItemListBoxSelectionFocused
        {
            get
            {
                return mDialogItemListBoxSelectionFocused;
            }
            set
            {
                mDialogItemListBoxSelectionFocused = value;
            }
        }
        
        private CanvasColor mDialogItemListBoxSelectionHighlighted;
        ///<summary>
        ///
        ///</summary>
        public CanvasColor DialogItemListBoxSelectionHighlighted
        {
            get
            {
                return mDialogItemListBoxSelectionHighlighted;
            }
            set
            {
                mDialogItemListBoxSelectionHighlighted = value;
            }
        }
        
        private CanvasColor mDialogItemListBoxSelectionFocusedHighlighted;
        ///<summary>
        ///
        ///</summary>
        public CanvasColor DialogItemListBoxSelectionFocusedHighlighted
        {
            get
            {
                return mDialogItemListBoxSelectionFocusedHighlighted;
            }
            set
            {
                mDialogItemListBoxSelectionFocusedHighlighted = value;
            }
        }
        
        #endregion
        

        public ColorScheme()
        {
        }
		
		public static IColorScheme Default => White;

        
        private static ColorScheme mWhite = null;

        public static IColorScheme White
        {
            get
            {
                if (mWhite == null)
                {
                    mWhite = new ColorScheme();
                    
                    mWhite.WindowBackground = new CanvasColor(0x70);
                    
                    mWhite.MenuItem = new CanvasColor(0x80);
                    
                    mWhite.MenuItemSelected = new CanvasColor(0x70);
                    
                    mWhite.MenuDisabledItem = new CanvasColor(0x87);
                    
                    mWhite.MenuDisabledItemSelected = new CanvasColor(0x78);
                    
                    mWhite.MenuHotKey = new CanvasColor(0x8e);
                    
                    mWhite.MenuHotKeySelected = new CanvasColor(0x7e);
                    
                    mWhite.DialogBorder = new CanvasColor(0x80);
                    
                    mWhite.DialogItemLabelColor = new CanvasColor(0x80);
                    
                    mWhite.DialogItemLabelHotKey = new CanvasColor(0x8e);
                    
                    mWhite.DialogItemCheckBoxColor = new CanvasColor(0x80);
                    
                    mWhite.DialogItemCheckBoxColorFocused = new CanvasColor(0x80);
                    
                    mWhite.DialogItemCheckBoxHotKey = new CanvasColor(0x8e);
                    
                    mWhite.DialogItemCheckBoxHotKeyFocused = new CanvasColor(0x8e);
                    
                    mWhite.DialogItemCheckBoxColorDisabled = new CanvasColor(0x87);
                    
                    mWhite.DialogItemButtonColor = new CanvasColor(0x80);
                    
                    mWhite.DialogItemButtonColorDisabled = new CanvasColor(0x87);
                    
                    mWhite.DialogItemButtonColorFocused = new CanvasColor(0x8f);
                    
                    mWhite.DialogItemButtonHotKey = new CanvasColor(0x8e);
                    
                    mWhite.DialogItemButtonHotKeyFocused = new CanvasColor(0x8e);
                    
                    mWhite.DialogItemEditColor = new CanvasColor(0x08);
                    
                    mWhite.DialogItemEditColorDisabled = new CanvasColor(0x07);
                    
                    mWhite.DialogItemEditColorFocused = new CanvasColor(0x0f);
                    
                    mWhite.DialogItemEditSelection = new CanvasColor(0x30);
                    
                    mWhite.DialogItemEditSelectionFocused = new CanvasColor(0x3f);
                    
                    mWhite.DialogItemListBoxColor = new CanvasColor(0x08);
                    
                    mWhite.DialogItemListBoxColorDisabled = new CanvasColor(0x07);
                    
                    mWhite.DialogItemListBoxColorFocused = new CanvasColor(0x0f);
                    
                    mWhite.DialogItemListBoxSelection = new CanvasColor(0x03);
                    
                    mWhite.DialogItemListBoxSelectionFocused = new CanvasColor(0xf3);
                    
                    mWhite.DialogItemListBoxSelectionHighlighted = new CanvasColor(0x0b);
                    
                    mWhite.DialogItemListBoxSelectionFocusedHighlighted = new CanvasColor(0xfb);
                    
			}
			return mWhite;
			}
		}
		
        private static ColorScheme mBlue = null;

        public static IColorScheme Blue
        {
            get
            {
                if (mBlue == null)
                {
                    mBlue = new ColorScheme();
                    
                    mBlue.WindowBackground = new CanvasColor(0x17);
                    
                    mBlue.MenuItem = new CanvasColor(0x3f);
                    
                    mBlue.MenuItemSelected = new CanvasColor(0x0f);
                    
                    mBlue.MenuDisabledItem = new CanvasColor(0x37);
                    
                    mBlue.MenuDisabledItemSelected = new CanvasColor(0x70);
                    
                    mBlue.MenuHotKey = new CanvasColor(0x3e);
                    
                    mBlue.MenuHotKeySelected = new CanvasColor(0x0e);
                    
                    mBlue.DialogBorder = new CanvasColor(0x30);
                    
                    mBlue.DialogItemLabelColor = new CanvasColor(0x30);
                    
                    mBlue.DialogItemLabelHotKey = new CanvasColor(0x3e);
                    
                    mBlue.DialogItemCheckBoxColor = new CanvasColor(0x30);
                    
                    mBlue.DialogItemCheckBoxColorFocused = new CanvasColor(0x30);
                    
                    mBlue.DialogItemCheckBoxHotKey = new CanvasColor(0x3e);
                    
                    mBlue.DialogItemCheckBoxHotKeyFocused = new CanvasColor(0x3e);
                    
                    mBlue.DialogItemCheckBoxColorDisabled = new CanvasColor(0x37);
                    
                    mBlue.DialogItemButtonColor = new CanvasColor(0x30);
                    
                    mBlue.DialogItemButtonColorDisabled = new CanvasColor(0x37);
                    
                    mBlue.DialogItemButtonColorFocused = new CanvasColor(0x3f);
                    
                    mBlue.DialogItemButtonHotKey = new CanvasColor(0x3e);
                    
                    mBlue.DialogItemButtonHotKeyFocused = new CanvasColor(0x3e);
                    
                    mBlue.DialogItemEditColor = new CanvasColor(0x03);
                    
                    mBlue.DialogItemEditColorDisabled = new CanvasColor(0x07);
                    
                    mBlue.DialogItemEditColorFocused = new CanvasColor(0x0b);
                    
                    mBlue.DialogItemEditSelection = new CanvasColor(0x10);
                    
                    mBlue.DialogItemEditSelectionFocused = new CanvasColor(0x1f);
                    
                    mBlue.DialogItemListBoxColor = new CanvasColor(0x03);
                    
                    mBlue.DialogItemListBoxColorDisabled = new CanvasColor(0x07);
                    
                    mBlue.DialogItemListBoxColorFocused = new CanvasColor(0x0b);
                    
                    mBlue.DialogItemListBoxSelection = new CanvasColor(0x10);
                    
                    mBlue.DialogItemListBoxSelectionFocused = new CanvasColor(0x1f);
                    
                    mBlue.DialogItemListBoxSelectionHighlighted = new CanvasColor(0x1e);
                    
                    mBlue.DialogItemListBoxSelectionFocusedHighlighted = new CanvasColor(0x0e);
                    
			}
			return mBlue;
			}
		}
		
        private static ColorScheme mBlack = null;

        public static IColorScheme Black
        {
            get
            {
                if (mBlack == null)
                {
                    mBlack = new ColorScheme();
                    
                    mBlack.WindowBackground = new CanvasColor(0x07);
                    
                    mBlack.MenuItem = new CanvasColor(0x0f);
                    
                    mBlack.MenuItemSelected = new CanvasColor(0x70);
                    
                    mBlack.MenuDisabledItem = new CanvasColor(0x07);
                    
                    mBlack.MenuDisabledItemSelected = new CanvasColor(0x78);
                    
                    mBlack.MenuHotKey = new CanvasColor(0x0e);
                    
                    mBlack.MenuHotKeySelected = new CanvasColor(0x7e);
                    
                    mBlack.DialogBorder = new CanvasColor(0x0f);
                    
                    mBlack.DialogItemLabelColor = new CanvasColor(0x07);
                    
                    mBlack.DialogItemLabelHotKey = new CanvasColor(0x0e);
                    
                    mBlack.DialogItemCheckBoxColor = new CanvasColor(0x07);
                    
                    mBlack.DialogItemCheckBoxColorFocused = new CanvasColor(0x0f);
                    
                    mBlack.DialogItemCheckBoxHotKey = new CanvasColor(0x0e);
                    
                    mBlack.DialogItemCheckBoxHotKeyFocused = new CanvasColor(0x0e);
                    
                    mBlack.DialogItemCheckBoxColorDisabled = new CanvasColor(0x08);
                    
                    mBlack.DialogItemButtonColor = new CanvasColor(0x07);
                    
                    mBlack.DialogItemButtonColorDisabled = new CanvasColor(0x08);
                    
                    mBlack.DialogItemButtonColorFocused = new CanvasColor(0x0f);
                    
                    mBlack.DialogItemButtonHotKey = new CanvasColor(0x0e);
                    
                    mBlack.DialogItemButtonHotKeyFocused = new CanvasColor(0x0e);
                    
                    mBlack.DialogItemEditColor = new CanvasColor(0x70);
                    
                    mBlack.DialogItemEditColorDisabled = new CanvasColor(0x78);
                    
                    mBlack.DialogItemEditColorFocused = new CanvasColor(0x70);
                    
                    mBlack.DialogItemEditSelection = new CanvasColor(0x10);
                    
                    mBlack.DialogItemEditSelectionFocused = new CanvasColor(0x1f);
                    
                    mBlack.DialogItemListBoxColor = new CanvasColor(0x70);
                    
                    mBlack.DialogItemListBoxColorDisabled = new CanvasColor(0x78);
                    
                    mBlack.DialogItemListBoxColorFocused = new CanvasColor(0x7f);
                    
                    mBlack.DialogItemListBoxSelection = new CanvasColor(0x10);
                    
                    mBlack.DialogItemListBoxSelectionFocused = new CanvasColor(0x1f);
                    
                    mBlack.DialogItemListBoxSelectionHighlighted = new CanvasColor(0x7e);
                    
                    mBlack.DialogItemListBoxSelectionFocusedHighlighted = new CanvasColor(0x1e);
                    
			}
			return mBlack;
			}
		}
		
    }
}
    