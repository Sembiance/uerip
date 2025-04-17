This was designed to be ran on a Linux system with dotnet.
It was also only tested on 1 game, Torchlight Infinite, so don't know how it'll handle other games.
Lastly it requires 2 binary DLLs be named in a very specific way for CUE4Parse to properly extract all files/textures.
One is built from 'detex' with info on how to build it below.
The other is 'oodle' and comes from Unreal Engine itself: cp UnrealEngine/Engine/Source/Programs/Shared/EpicGames.Oodle/Sdk/2.9.3/linux/lib/liboo2corelinux64.so.9 ./dll/oo2core_9_win64.dll

Clone the repo recursively:
```bash
git clone --recursive https://github.com/Sembiance/uerip.git
cd uerip
cd CUE4Parse
git submodule update --init --recursive
git apply ../oodle_zlib.patch
cd ..
```

Modify and build detex:
```bash
cd detex
git apply ../detex.patch
make
cp libdetex.so.0.1.2 ../dll/Detex.dll
```

Then you can build ueListFiles and uerip:
```bash
cd ueListFiles
./build.sh
cd ../uerip
./build.sh
```

Use `ueListFiles` to list all the files in UE paks and `uerip` to rip them.

NOTE: CUE4Parse tends to crash, thus if you try and use `uerip` to extract multiple files at once, if it crashes on any 1 file you miss all the others.
So I highly recommend listing the files first with `ueListFiles`, then call `uerip --fileid=...` for each and every file individually.

You can examine `uerip.js` for how I do it but note that it uses deno and some external code not provided to run, so just use it as an example for the script that you will write.

Finally, if you need an AESKey for your game, try extracting it with this script (not mine): https://gist.github.com/Sembiance/1c99081042ad41c53f3c8f30ac5de1ba
