#!/bin/bash
rm -rf bin/*
dotnet build --configuration Release
dotnet publish --configuration Release
cp ../dll/*.dll bin/Release/net*/publish/
cp ../dll/*.dll bin/Release/net*/
chmod 755 bin/Release/net*/Detex.dll bin/Release/net*/publish/Detex.dll