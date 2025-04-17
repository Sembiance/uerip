#!/bin/bash
rm -rf bin/*
dotnet build --configuration Release
dotnet publish --configuration Release

netVersion=$(basename bin/Release/net*/)

cp ../dll/*.dll bin/Release/"$netVersion"/publish/
cp ../dll/*.dll bin/Release/"$netVersion"/
chmod 755 bin/Release/"$netVersion"/Detex.dll bin/Release/"$netVersion"/publish/Detex.dll