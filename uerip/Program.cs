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

// Will need to set up project with: dotnet add package System.CommandLine --prerelease

namespace UERip
{
	class Program
	{
		static async Task<int> Main(string[] args)
		{
			var aesKeyOption = new Option<string>("--aesKey", "AES key"){ IsRequired = false };
			var skipSlowOption = new Option<bool>("--skipSlow", "Skip slow processing (meshes, animations, skeletons)."){ IsRequired = false };
			var pakDirOption = new Option<string>("--pakDir", "PAK directory path"){ IsRequired = true };
			var outDirOption = new Option<string>("--outDir", "Output directory path"){ IsRequired = true };
			var fileidTargetOption = new Option<string>("--fileid", "Just rip one fileid"){ IsRequired = false };
			var ueVersionOption = new Option<string>("--ueVersion", "Which Unreal Engine version to specify. Allowed: 4.x, 5.x, torchlightInfinite  Default: 4.x"){ IsRequired = false };

			var rootCommand = new RootCommand("Rips all assets from any PAK files found in pakDir to outDir");
			rootCommand.AddOption(pakDirOption);
			rootCommand.AddOption(outDirOption);
			rootCommand.AddOption(aesKeyOption);
			rootCommand.AddOption(skipSlowOption);
			rootCommand.AddOption(fileidTargetOption);
			rootCommand.AddOption(ueVersionOption);

			rootCommand.SetHandler((pakDir, outDir, aesKey, skipSlow, fileidTarget, ueVersion) =>
			{
				HashSet<string> knownOtherClasses = new HashSet<string> { "AkAssetPlatformData", "AnimBoneCompressionSettings", "AnimCurveCompressionSettings", "BlendSpace1D", "CompositeDataTable", "CurveLinearColor", "DataTable", "FileMediaSource", "Font", "FontFace", "MapBuildDataRegistry", "MediaTexture", "PhysicsAsset", "SoundCue", "TextureCube", "TextureRenderTarget2D", "UserDefinedStruct", "VolumeTexture" };
				SKPngEncoderOptions textureEncodeOptions = new SKPngEncoderOptions(SKPngEncoderFilterFlags.NoFilters, 1);
				DirectoryInfo outDirInfo = new DirectoryInfo(outDir);

				var ueVersionContainer = ueVersion switch
				{
					"4.x" => new VersionContainer(EGame.GAME_UE4_LATEST),
					"5.x" => new VersionContainer(EGame.GAME_UE5_LATEST),
					"torchlightInfinite" => new VersionContainer(EGame.GAME_TorchlightInfinite),
					_ => new VersionContainer(EGame.GAME_UE4_LATEST)
				};
				
				var provider = new DefaultFileProvider(pakDir, SearchOption.TopDirectoryOnly, false, ueVersionContainer);
				provider.Initialize();
				if(aesKey!=null)
					provider.SubmitKey(new FGuid(), new FAesKey(aesKey));
				//provider.LoadLocalization(ELanguage.English);

				HashSet<string> seenOtherClasses = new HashSet<string>();

				foreach(string fileid in provider.Files.Keys)
				{	
					string ext = Path.GetExtension(fileid);
					if(ext==".uexp" || ext==".ubulk")
						continue;
					
					if(fileidTarget!=null && !fileid.StartsWith(fileidTarget))
						continue;

					string packageid = Path.ChangeExtension(fileid, null);
					Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(outDir, packageid)));
					
					if(ext==".uasset")
					{
						try
						{
							var obj = provider.LoadObject(packageid);
							switch(obj)
							{
								case UTexture2D texture:
									string pngFilePath = Path.Combine(outDir, packageid) + ".png";
									Console.WriteLine("\x1b[92mCONVERTING\x1b[96m:\x1b[0m UTexture2D: " + packageid);

									using(var pixmap= TextureDecoder.Decode(texture).PeekPixels())
									{
										using(var data = pixmap.Encode(textureEncodeOptions))
											File.WriteAllBytes(pngFilePath, data.ToArray());
									}

									//using(Stream s = File.OpenWrite(pngFilePath))
									//	TextureDecoder.Decode(texture).Encode(SKEncodedImageFormat.Png, 100).SaveTo(s);
									continue;
								
								case USoundWave:
								case UAkMediaAssetData:
									Console.WriteLine("\x1b[92m CONVERTING\x1b[96m:\x1b[0m " + obj.Class.ToString() + ": " + packageid);
									SoundDecoder.Decode(obj, true, out string audioFormat, out byte[] soundData);
									File.WriteAllBytes(Path.Combine(outDir, Path.ChangeExtension(fileid, "." + audioFormat.ToLower())), soundData);
									continue;

								case UStringTable table:
									Console.WriteLine("\x1b[92m EXPORTING\x1b[96m:\x1b[0m UStringTable: " + packageid);
									File.WriteAllText(Path.Combine(outDir, Path.ChangeExtension(fileid, ".json")), JsonSerializer.Serialize(table.StringTable.KeysToMetaData));
									continue;
								
								case UUserDefinedEnum:
									Console.WriteLine("\x1b[92m EXPORTING\x1b[96m:\x1b[0m UUserDefinedEnum: " + packageid);
									Dictionary<string, long> enumMap = new Dictionary<string, long>();
									foreach(var (name, enumValue) in ((UEnum)obj).Names)
										enumMap.Add(name.Text, enumValue);
									File.WriteAllText(Path.Combine(outDir, Path.ChangeExtension(fileid, ".json")), JsonSerializer.Serialize(enumMap));
									continue;
								
								//case UMaterialInterface:
								//	Console.WriteLine("\x1b[92m EXPORTING\x1b[96m:\x1b[0m " + obj.Class.ToString() + ": " + packageid);
								//	new Exporter(obj, new ExporterOptions()).TryWriteToDir(outDirInfo, out string _, out string _);
								//	continue;

								case UMaterialInterface:
								case UStaticMesh:
								case USkeletalMesh:
								case USkeleton:
								case UAnimSequence:
								case UAnimMontage:
								case UAnimComposite:
									if(!skipSlow)
									{
										Console.WriteLine("\x1b[92m EXPORTING\x1b[96m:\x1b[0m " + obj.Class.ToString() + ": " + packageid);
										new Exporter(obj, new ExporterOptions()).TryWriteToDir(outDirInfo, out string _, out string _);
									}
									continue;

								default:
									if(!knownOtherClasses.Contains(obj.Class.ToString()) && obj.GetType().ToString()!="CUE4Parse.UE4.Assets.Exports.UObject")
									{
										Console.WriteLine("\x1b[91m     OTHER\x1b[96m:\x1b[0m (Class: " + obj.Class.ToString() + ") (Type: " + obj.GetType()+ "): " + packageid);
										seenOtherClasses.Add(obj.Class.ToString());
									}
									break;
							}
						}
						catch(Exception ex)
						{
							if(!ex.Message.Contains("does not have an export with the name"))
								Console.WriteLine("\x1b[91mUNEXPECTED\x1b[96m:\x1b[0m " + ex.GetType() + " EXCEPTION (" + packageid + "): " + ex.Message);
						}
					}

					Console.WriteLine("\x1b[38;5;208m   WRITING\x1b[96m:\x1b[0m " + fileid);
					File.WriteAllBytes(Path.Combine(outDir, fileid), provider.Files[fileid].Read());
				}

				// pass 2. For any file that we have a .uasset, either append .uexp data to it or output .ubulk file
				foreach(string fileid in provider.Files.Keys)
				{	
					if(fileidTarget!=null && !fileid.StartsWith(fileidTarget))
						continue;

					string uassetFilePath = Path.Combine(outDir, Path.ChangeExtension(fileid, ".uasset"));
					if(!File.Exists(uassetFilePath))
						continue;

					string ext = Path.GetExtension(fileid);
					if(ext==".uexp")
					{
						Console.WriteLine("\x1b[38;5;208m APPENDING\x1b[96m:\x1b[0m " + Path.ChangeExtension(fileid, ".uasset"));
						byte[] fileData = provider.Files[fileid].Read();
						using(FileStream fs = new FileStream(uassetFilePath, FileMode.Append))
							fs.Write(fileData, 0, fileData.Length);

					}
					else if(ext==".ubulk")
					{
						Console.WriteLine("\x1b[38;5;208m   WRITING\x1b[96m:\x1b[0m " + fileid);
						File.WriteAllBytes(Path.Combine(outDir, fileid), provider.Files[fileid].Read());
					}
				}

				foreach(string otherClass in seenOtherClasses)
					Console.WriteLine("\x1b[91m NEW OTHER\x1b[96m:\x1b[0m (Class: " + otherClass + ")");
			}, pakDirOption, outDirOption, aesKeyOption, skipSlowOption, fileidTargetOption, ueVersionOption);

			return await rootCommand.InvokeAsync(args);
		}
	}
}
