# example download link for testing
# http://localhost:3000/download?fileid=test.jpg&authtoken=132303

# publish command for windows
dotnet publish -r win-x64 -c Release -p:PublishSingleFile=true,AssemblyName=urlhandler --no-self-contained