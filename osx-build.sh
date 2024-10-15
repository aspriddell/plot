rm -rf Plot.app
rm -f Plot.app.zip

# prepare the app bundle
mkdir -p Plot.app/Contents/MacOS
cp Info.plist Plot.app/Contents

# build directly into the app bundle
dotnet publish -c Release -p:Version=$1 --self-contained -r osx-arm64 -o ./Plot.app/Contents/MacOS Plot/Plot.csproj

# perform signing with timestamp
codesign --sign "Developer ID Application: Albie Spriddell (Q824VHAT9S)" \
  --entitlements app.entitlements \
  --options=runtime \
  --timestamp \
  --no-strict \
  --force \
  --deep \
  Plot.app

# create a zip file
ditto -ck --sequesterRsrc --keepParent Plot.app Plot.app.zip

# submit for notarization
xcrun notarytool submit Plot.app.zip \
  --keychain-profile "notarytool" \
  --progress \
  --wait
  
# staple the notarization ticket and rearchive
xcrun stapler staple Plot.app
ditto -ck --sequesterRsrc --keepParent Plot.app Plot.app.zip
