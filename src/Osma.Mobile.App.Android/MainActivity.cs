using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Acr.UserDialogs;
using Android;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Autofac;
using FFImageLoading.Forms.Platform;
using Java.Lang;
using Xamarin.Forms;
using Plugin.CurrentActivity;
using Plugin.Fingerprint;

namespace Osma.Mobile.App.Droid
{
    [Activity(Label = "Osma", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(bundle);
            CrossCurrentActivity.Current.Init(this.Application);

            CrossFingerprint.SetCurrentActivityResolver(() => CrossCurrentActivity.Current.Activity);

            Forms.Init(this, bundle);

            XF.Material.Droid.Material.Init(this, bundle);

            Rg.Plugins.Popup.Popup.Init(this, bundle);

            // Initializing FFImageLoading
            CachedImageRenderer.Init(false);

            // Initializing User Dialogs
            UserDialogs.Init(this);

            // Initializing Xamarin Essentials
            Xamarin.Essentials.Platform.Init(this, bundle);

#if GORILLA
            LoadApplication(UXDivers.Gorilla.Droid.Player.CreateApplication(
                this,
                new UXDivers.Gorilla.Config("Good Gorilla")
                .RegisterAssemblyFromType<InverseBooleanConverter>()
                .RegisterAssemblyFromType<CachedImageRenderer>()));
#else
            //Loading dependent libindy
            JavaSystem.LoadLibrary("gnustl_shared");
            JavaSystem.LoadLibrary("indy");

            // Initializing QR Code Scanning support
            ZXing.Net.Mobile.Forms.Android.Platform.Init();

            //Marshmellow and above require permission requests to be made at runtime
            if ((int)Build.VERSION.SdkInt >= 23)
                CheckAndRequestRequiredPermissions();

            var builder = new ContainerBuilder();
            builder.RegisterModule(new PlatformModule());
            var container = builder.Build();

            

            // TODO: Implement the same for IOS

            // Read genesis file from asset folder as bytes
            Stream input = Assets.Open("pool_genesis.Remote.txn");
            byte[] buffer = ReadFully(input);

            // Write genesis file to internal storage (can be accessed only by our application)
            var genesisFilePath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "pool_genesis.Remote.txn");
            bool fileExists = File.Exists(genesisFilePath);
            if (fileExists) File.Delete(genesisFilePath);
            BinaryWriter writer = new BinaryWriter(File.Open(genesisFilePath, FileMode.OpenOrCreate));
            writer.Write(buffer);
            writer.Flush();
            writer.Close();


            LoadApplication(new App(container));
        #endif
        }

        private static byte[] ReadFully(Stream stream)
        {
            byte[] buffer = new byte[32768];
            using (MemoryStream ms = new MemoryStream())
            {
                while (true)
                {
                    int read = stream.Read(buffer, 0, buffer.Length);
                    if (read <= 0)
                        return ms.ToArray();
                    ms.Write(buffer, 0, read);
                }
            }
        }

        readonly string[] _permissionsRequired =
        {
            Manifest.Permission.ReadExternalStorage,
            Manifest.Permission.WriteExternalStorage,
            Manifest.Permission.Camera
        };

        private int _requestCode = -1;
        private List<string> _permissionsToBeGranted = new List<string>();

        private void CheckAndRequestRequiredPermissions()
        {
            for (int i = 0; i < _permissionsRequired.Length; i++)
                if (CheckSelfPermission(_permissionsRequired[i]) != (int)Permission.Granted)
                    _permissionsToBeGranted.Add(_permissionsRequired[i]);

            if (_permissionsToBeGranted.Any())
            {
                _requestCode = 10;
                RequestPermissions(_permissionsRequired.ToArray(), _requestCode);
            }
            else
                System.Diagnostics.Debug.WriteLine("Device already has all the required permissions");
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions,
            Permission[] grantResults)
        {
            if (grantResults.Length == _permissionsToBeGranted.Count)
                System.Diagnostics.Debug.WriteLine("All permissions required that werent granted, have now been granted");
            else
                System.Diagnostics.Debug.WriteLine("Some permissions requested were denied by the user");
           
           Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
           base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}
