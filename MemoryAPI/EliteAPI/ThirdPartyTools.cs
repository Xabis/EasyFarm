using System;
using System.Text;

namespace EliteMMO.API
{
    public class ThirdPartyTools
    {
        private readonly IntPtr _instance;

        public ThirdPartyTools(IntPtr apiObject)
        {
            _instance = apiObject;
        }

        public void CreateTextObject(string name)
        {
            EliteAPI.CreateTextObject(_instance, name);
        }

        public void DeleteTextObject(string name)
        {
            EliteAPI.DeleteTextObject(_instance, name);
        }

        public void SetText(string name, string text)
        {
            EliteAPI.SetText(_instance, name, text);
        }

        public void SetVisibility(string name, bool visible)
        {
            EliteAPI.SetVisibility(_instance, name, visible);
        }

        public void SetFont(string name, string font, int height)
        {
            EliteAPI.SetFont(_instance, name, font, height);
        }

        public void SetColor(string name, byte a, byte r, byte g, byte b)
        {
            EliteAPI.SetColor(_instance, name, a, r, g, b);
        }

        public void SetLocation(string name, float x, float y)
        {
            EliteAPI.SetLocation(_instance, name, x, y);
        }

        public void SetBold(string name, bool bold)
        {
            EliteAPI.SetBold(_instance, name, bold);
        }

        public void SetItalic(string name, bool italic)
        {
            EliteAPI.SetItalic(_instance, name, italic);
        }

        public void SetBGColor(string name, byte a, byte r, byte g, byte b)
        {
            EliteAPI.SetBGColor(_instance, name, a, r, g, b);
        }

        public void SetBGBorderSize(string name, float size)
        {
            EliteAPI.SetBGBorderSize(_instance, name, size);
        }

        public void SetBGVisibility(string name, bool visible)
        {
            EliteAPI.SetBGVisibility(_instance, name, visible);
        }

        public void SetRightJustified(string name, bool rightjustified)
        {
            EliteAPI.SetRightJustified(_instance, name, rightjustified);
        }

        public void FlushCommands()
        {
            EliteAPI.FlushCommands(_instance);
        }

        public void SetKey(byte key, bool down)
        {
            EliteAPI.SetKey(_instance, key, down);
        }

        public void BlockInput(bool block)
        {
            EliteAPI.BlockInput(_instance, block);
        }

        public void SendString(string str)
        {
            EliteAPI.SendString(_instance, str);
        }

        public void KeyDown(byte key)
        {
            SetKey(key, down: true);
        }

        public void KeyUp(byte key)
        {
            SetKey(key, down: false);
        }

        public void KeyPress(byte key)
        {
            SetKey(key, down: true);
            SetKey(key, down: false);
        }

        public void KeyDown(Keys key)
        {
            SetKey((byte)key, down: true);
        }

        public void KeyUp(Keys key)
        {
            SetKey((byte)key, down: false);
        }

        public void KeyPress(Keys key)
        {
            SetKey((byte)key, down: true);
            SetKey((byte)key, down: false);
        }

        public int ConsoleIsNewCommand()
        {
            return EliteAPI.ConsoleIsNewCommand(_instance);
        }

        public int ConsoleGetArgCount()
        {
            return EliteAPI.ConsoleGetArgCount(_instance);
        }

        public string ConsoleGetArg(int index)
        {
            byte[] array = new byte[4096];
            if (EliteAPI.ConsoleGetArg(_instance, index, array, 4096) != 0)
            {
                return Encoding.ASCII.GetString(array).Trim(default(char));
            }

            return string.Empty;
        }
    }
}