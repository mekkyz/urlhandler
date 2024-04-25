$APP_NAME = "URL Handler"
$OUTPUT_DIR = "output"
$MSI_NAME = "$APP_NAME.msi"
$LICENSE_FILE = "./LICENSE.rtf"

# build and pack app
dotnet clean
dotnet build --configuration Release
dotnet publish -r win-x64 -c Release -p:PublishSingleFile=$true,AssemblyName=urlhandler --no-self-contained

# create directory for WiX && app files && license files
New-Item -ItemType Directory -Force -Path $OUTPUT_DIR
Copy-Item "bin\Release\net6.0-windows10.0.17763.0\win-x64\publish\*" $OUTPUT_DIR -Recurse
Copy-Item $LICENSE_FILE $OUTPUT_DIR

# add WiX xml config file
$configContent = @"
<?xml version='1.0'?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">
  <Product Id='*' Name='$APP_NAME' Language='1033' Version='1.0.0.0' Manufacturer='SCC' UpgradeCode='PUT-GUID-HERE'>
    <Package InstallerVersion='200' Compressed='yes' InstallScope='perMachine' />
    <MajorUpgrade DowngradeErrorMessage='A newer version of $APP_NAME is already installed.' />
    <MediaTemplate EmbedCab='yes' />
    
    <!-- License File Configuration -->
    <WixVariable Id='WixUILicenseRtf' Value='LICENSE.rtf'/>
    <UIRef Id='WixUI_Mondo' />
    <UIRef Id='WixUI_ErrorProgressText' />

    <Directory Id='TARGETDIR' Name='SourceDir'>
      <Directory Id='ProgramFilesFolder'>
        <Directory Id='INSTALLFOLDER' Name='$APP_NAME'>
          <!-- Components for application files and registry entries -->
          <Component Id='ApplicationFiles' Guid='*'>
            <File Id='AppExecutable' Source='urlhandler.exe' KeyPath='yes' />
            <File Id='AppIcon' Source='icon.ico' />
            <!-- Registry entries -->
            <RegistryKey Root='HKCR' Key='localhost' Action='createAndRemoveOnUninstall'>
              <RegistryValue Type='string' Name='' Value='URL:Localhost Protocol' />
              <RegistryValue Type='string' Name='URL Protocol' Value='' />
              <RegistryKey Key='DefaultIcon' Action='createAndRemoveOnUninstall'>
                <RegistryValue Type='string' Value='[INSTALLFOLDER]icon.ico' />
              </RegistryKey>
              <RegistryKey Key='shell\open\command' Action='createAndRemoveOnUninstall'>
                <RegistryValue Type='string' Value='`"[INSTALLFOLDER]urlhandler.exe`" `%1`' />
              </RegistryKey>
            </RegistryKey>
          </Component>
        </Directory>
      </Directory>
    </Directory>

    <Feature Id='ProductFeature' Title='$APP_NAME' Level='1'>
      <ComponentRef Id='ApplicationFiles' />
    </Feature>
  </Product>
</Wix>
"@ 

$configContent | Out-File "$OUTPUT_DIR/Product.wxs"

# compile config file and create msi file
wix build -nologo -arch x64 -o "$OUTPUT_DIR/$MSI_NAME" "$OUTPUT_DIR/Product.wxs"

Write-Host "MSI installer created successfully: $OUTPUT_DIR/$MSI_NAME"
