#!/bin/bash

# variables
APP_NAME="URL Handler"
OUTPUT_DIR="output"
MSI_NAME="$APP_NAME.msi"
REG_FILE="./file.reg"

# build and pack app
dotnet clean
dotnet build --configuration Release
dotnet publish -r win-x64 -c Release /p:PublishSingleFile=true /p:AssemblyName=urlhandler /p:PublishTrimmed=true --no-self-contained

# directory for WiX && add app files and reg file
mkdir -p "$OUTPUT_DIR"
cp -r bin\Release\net6.0-windows10.0.17763.0\win-x64\publish/* "$OUTPUT_DIR"
cp "$REG_FILE" "$OUTPUT_DIR"

# add WiX xml config file
cat <<EOF >"$OUTPUT_DIR/Product.wxs"
<?xml version='1.0'?>
<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>
  <Product Id='*' Name='$APP_NAME' Language='1033' Version='1.0.0.0' Manufacturer='YourCompany' UpgradeCode='PUT-GUID-HERE'>
    <Package InstallerVersion='200' Compressed='yes' InstallScope='perMachine' />

    <MajorUpgrade DowngradeErrorMessage='A newer version of $APP_NAME is already installed.' />
    
    <MediaTemplate EmbedCab='yes' />

    <Feature Id='ProductFeature' Title='$APP_NAME' Level='1'>
      <ComponentGroupRef Id='ProductComponents' />
    </Feature>
  </Product>

  <Fragment>
    <Directory Id='TARGETDIR' Name='SourceDir'>
      <Directory Id='ProgramFilesFolder'>
        <Directory Id='INSTALLFOLDER' Name='$APP_NAME' />
      </Directory>
    </Directory>
  </Fragment>

  <Fragment>
    <ComponentGroup Id='ProductComponents' Directory='INSTALLFOLDER'>
      <!-- Add your application files here -->
      <Component Id='MainExecutable' Guid='PUT-GUID-HERE'>
        <File Id='MainExecutableFile' Source='YourAppExecutable.exe' KeyPath='yes' Checksum='yes' />
      </Component>
      <!-- Add your .reg file to register application -->
      <Component Id='RegisterRegFile' Guid='PUT-GUID-HERE'>
        <File Id='RegFile' Source='registration_file.reg' KeyPath='yes' />
      </Component>
    </ComponentGroup>
  </Fragment>

  <!-- Define custom action to execute .reg file -->
  <Fragment>
    <CustomAction Id='ExecuteRegFile' FileKey='RegFile' ExeCommand='/s' Execute='deferred' Return='ignore' />
    <InstallExecuteSequence>
      <Custom Action='ExecuteRegFile' After='InstallFiles'>NOT Installed</Custom>
    </InstallExecuteSequence>
  </Fragment>
</Wix>
EOF

# compile config file and create msi file
candle -nologo -arch x64 -ext WixNetFxExtension "$OUTPUT_DIR/Product.wxs"
light -nologo -ext WixNetFxExtension -sice:ICE60 -sice:ICE61 -out "$OUTPUT_DIR/$MSI_NAME" "$OUTPUT_DIR/Product.wixobj"

echo "MSI installer created successfully: $OUTPUT_DIR/$MSI_NAME"
