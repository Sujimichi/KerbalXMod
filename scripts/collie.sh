#Collie - rounds all the files up into release

rm -rf bin/Release/KerbalX
rm bin/Release/KerbalXMod.zip

mkdir bin/Release/KerbalX -p
mkdir bin/Release/KerbalX/Plugins -p
mkdir bin/Release/KerbalX/Assets -p

cp bin/Release/KerbalX.dll bin/Release/KerbalX/Plugins/KerbalX.dll
cp -a assets/images/*.png bin/Release/KerbalX/Assets/
cp LICENCE.txt bin/Release/KerbalX/LICENCE.txt

ruby -e "i=%x(cat Source/KerbalX.cs | grep version); i=i.split('=')[1].sub(';','').gsub('\"','').strip; s=\"echo 'version: #{i}' > bin/Release/KerbalX/version\"; system(s)"


rm bin/Release/KerbalX.dll
rm bin/Release/KerbalX.dll.mdb

cd bin/Release
zip -r KerbalXMod.zip KerbalX/

rm -rf /home/sujimichi/KSP/dev_KSP-1.3.1/GameData/KerbalX/
cp -R KerbalX/ /home/sujimichi/KSP/dev_KSP-1.3.1/GameData/KerbalX/

rm -rf /home/sujimichi/Share/KX_mod_dev/KerbalX/
cp -R KerbalX/ /home/sujimichi/Share/KX_mod_dev/KerbalX/
