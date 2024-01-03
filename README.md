Clone the repo recursively:
```bash
git clone --recursive https://github.com/Sembiance/uerip.git
```

Modify and build detex:
```bash
cd uerip
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
