#Collie - rounds all the files up into release

rm -rf bin/Release/KerbalX

mkdir bin/Release/KerbalX -p
mkdir bin/Release/KerbalX/Plugins -p
mkdir bin/Release/KerbalX/Assets -p

cp bin/Release/KerbalX.dll bin/Release/KerbalX/Plugins/KerbalX.dll
cp -a assets/images/*.png bin/Release/KerbalX/Assets/

rm bin/Release/KerbalX.dll
rm bin/Release/KerbalX.dll.mdb
