using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace Yeelight_Controller
{
    public class KBShortcut
    {
        public ModifierKeys modifiers;
        public Key key;
        public string name;
        public string description;

        public KBShortcut(string name, ModifierKeys modifiers, Key pressedKey)
        {
            var converter = new KeysConverter();
            string modifierString = converter.ConvertToString(modifiers);
            string key = converter.ConvertToString(pressedKey);

            // Supress unwanted keys
            if (key == "LeftCtrl" || key == "RightCtrl" || key == "LeftShift" || key == "RightShift" || key == "System" || key == "LWin" || key == "RWin")
                key = "";

            this.name = name;
            this.modifiers = modifiers;
            this.key = pressedKey;
            this.description = modifierString + " + " + key;
        }

        public KBShortcut() { } // Required to serialize class
    }
}
