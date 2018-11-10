#Collie - rounds all the files up into release

rm -rf bin/Release/KerbalX

mkdir bin/Release/KerbalX -p
mkdir bin/Release/KerbalX/Plugins -p
mkdir bin/Release/KerbalX/Assets -p

cp bin/Release/KerbalX.dll bin/Release/KerbalX/Plugins/KerbalX.dll

cp -a assets/images/*.png bin/Release/KerbalX/Assets/

cp LICENCE.txt bin/Release/KerbalX/LICENCE.txt


MODVER=$(ruby -e "i=%x(cat Source/KerbalX.cs | grep version); i=i.split(';')[0].split('=')[1].sub(';','').gsub('\"','').strip; puts i;")
KSPVER=$(ruby -e "i=%x(cat Source/KerbalX.cs | grep 'Built Against KSP'); i=i.split(' ').last; puts i")

echo "version $MODVER" > bin/Release/KerbalX/version

rm bin/Release/*.dll
rm bin/Release/*.dll.mdb

cd bin/Release
rm -rf $MODVER/

mkdir $MODVER
#rm KerbalX_$MODVER.zip
zip -r $MODVER/KerbalX.zip KerbalX/

rm -rf /home/sujimichi/KSP/dev_KSP-$KSPVER/GameData/KerbalX/
cp -R KerbalX/ /home/sujimichi/KSP/dev_KSP-$KSPVER/GameData/KerbalX/
