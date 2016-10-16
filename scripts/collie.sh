#Collie - rounds all the files up into release

rm -rf bin/Release/KerbalX
rm bin/Release/KerbalXMod.zip

mkdir bin/Release/KerbalX -p
mkdir bin/Release/KerbalX/Plugins -p
mkdir bin/Release/KerbalX/Assets -p

cp bin/Release/KerbalX.dll bin/Release/KerbalX/Plugins/KerbalX.dll
cp -a assets/images/*.png bin/Release/KerbalX/Assets/
cp LICENCE.txt bin/Release/KerbalX/LICENCE.txt

rm bin/Release/KerbalX.dll
rm bin/Release/KerbalX.dll.mdb

cd bin/Release
zip -r KerbalXMod.zip KerbalX/

