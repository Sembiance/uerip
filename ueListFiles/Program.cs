using System;
using System.Collections.Generic;
using System.CommandLine;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using SkiaSharp;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Assets.Exports.Internationalization;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Exports.Wwise;
using CUE4Parse.UE4.Objects;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using CUE4Parse_Conversion;
using CUE4Parse_Conversion.Sounds;
using CUE4Parse_Conversion.Textures;

namespace UEListFiles
{
	class Program
	{
		static async Task<int> Main(string[] args)
		{
			var aesKeyOption = new Option<string>("--aesKey", "AES key"){ IsRequired = false };
			var pakDirOption = new Option<string>("--pakDir", "PAK directory path"){ IsRequired = true };
			var ueVersionOption = new Option<string>("--ueVersion", "Which Unreal Engine version to specify. Allowed: 4.x, 5.x, torchlightInfinite  Default: 4.x"){ IsRequired = false };

			var rootCommand = new RootCommand("Lists all files found within any PAK files found in pakDir");
			rootCommand.AddOption(pakDirOption);
			rootCommand.AddOption(aesKeyOption);
			rootCommand.AddOption(ueVersionOption);

			rootCommand.SetHandler((pakDir, aesKey, ueVersion) =>
			{
				var ueVersionContainer = ueVersion switch
				{
					"4.x" => new VersionContainer(EGame.GAME_UE4_LATEST),
					"5.x" => new VersionContainer(EGame.GAME_UE5_LATEST),
					"torchlightInfinite" => new VersionContainer(EGame.GAME_TorchlightInfinite),
					"titanQuest2" => new VersionContainer(EGame.GAME_UE5_2),
					_ => new VersionContainer(EGame.GAME_UE4_LATEST)
				};
				
				var provider = new DefaultFileProvider(pakDir, SearchOption.TopDirectoryOnly, false, ueVersionContainer);
				provider.Initialize();
				if(aesKey!=null)
					provider.SubmitKey(new FGuid(), new FAesKey(aesKey));
				//provider.LoadLocalization(ELanguage.English);

				foreach(string fileid in provider.Files.Keys)
					Console.WriteLine(fileid);
			}, pakDirOption, aesKeyOption, ueVersionOption);

			return await rootCommand.InvokeAsync(args);
		}
	}
}
