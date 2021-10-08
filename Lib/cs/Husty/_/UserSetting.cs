﻿using System.IO;
using System.Text.Json;

namespace Husty
{
    public class UserSetting<T>
    {

        private readonly T _defaultValue;

        public UserSetting(T defaultValue)
        {
            _defaultValue = defaultValue;
        }
        
        public T Load()
        {
            try
            {
                return JsonSerializer.Deserialize<T>(File.ReadAllText("setting.json"));
            }
            catch 
            {
                return _defaultValue;
            }
        }

        public void Save(T value)
        {
            using var sw = new StreamWriter("setting.json", false);
            sw.WriteLine(JsonSerializer.Serialize(value));
        }

    }
}