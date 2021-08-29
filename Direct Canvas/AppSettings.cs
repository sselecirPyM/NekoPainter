using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace DirectCanvas
{
    public class AppSettings
    {
        public static AppSettings LoadDefault()
        {
            AppSettings settings = new AppSettings();
            settings.container = ApplicationData.Current.RoamingSettings.CreateContainer("Settings", ApplicationDataCreateDisposition.Always);

            if (settings.container.Values.TryGetValue("Color", out object backgroundColor))
            {
                string[] a = backgroundColor.ToString().Split(' ');
                Vector4 c = new Vector4(float.Parse(a[0]), float.Parse(a[1]), float.Parse(a[2]), float.Parse(a[3]));
                settings.BackGroundColor = c;
            }
            else
            {
                var c = new Vector4(0.392156899f, 0.584313750f, 0.929411829f, 1.000000000f);
                settings.BackGroundColor = c;
                settings.container.Values["Color"] = string.Format("{0} {1} {2} {3}", c.X, c.Y, c.Z, c.W);
            }
            return settings;
        }

        public AppSettings()
        {

        }

        ApplicationDataContainer container;

        public Vector4 BackGroundColor;

    }
}
