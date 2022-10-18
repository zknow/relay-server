using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using RelayServer.Models;
using Serilog;

namespace RelayServer;

// Doc: https://www.rocksaying.tw/archives/2019/dotNET-Core-%E7%AD%86%E8%A8%98-ASP.NET-Core-appsettings.json-%E8%88%87%E5%9F%B7%E8%A1%8C%E7%92%B0%E5%A2%83.html
public class Config
{
    public static AppSettings? AppSetting;
    private static IConfigurationRoot? conf;

    public static void LoadConfig()
    {
        AppSetting = new AppSettings();

        var builder = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        conf = builder.Build();
        conf.Bind(AppSetting);

        //使用從 appsettings.json 讀取到的內容來設定 logger
        Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(conf)
                    .CreateLogger();

        // ChangeToken.OnChange(conf.GetReloadToken, () =>
        // {
        //     Thread.Sleep(2000);
        //     conf.Bind(AppSetting);
        //     Console.WriteLine($"Configuration changed:{AppSetting.Port}");
        // });
    }

    public static string GetValueFromKey(string key)
    {
        return conf.GetSection(key).Value;
    }
}