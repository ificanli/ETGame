$platform = "linux-x64"

function PublishLinux {
    dotnet publish ET.sln -r $platform --no-self-contained --no-dependencies -c Debug

    $path = "Publish/"
    Remove-Item $path/ -Recurse -ErrorAction Ignore

    Move-Item ./Bin/$platform/publish/* Publish/Bin/ -Force

    $dest2 = "Publish/Packages/cn.etetet.loader/Scripts/Loader/Server"
    if (!(Test-Path $dest2)) { New-Item -ItemType Directory -Path $dest2 -Force | Out-Null }
    Copy-Item Packages/cn.etetet.loader/Scripts/Loader/Server/NLog.config -Destination $dest2 -Force

    $dest3 = "Publish/Packages/cn.etetet.wow/Bundles/"
    if (!(Test-Path $dest3)) { New-Item -ItemType Directory -Path $dest3 -Force | Out-Null }
    Copy-Item Packages/cn.etetet.wow/Bundles/Bson/ -Destination $dest3 -Recurse -Force

    $dest4 = "Publish/Packages/cn.etetet.wow/Bundles/"
    if (!(Test-Path $dest4)) { New-Item -ItemType Directory -Path $dest4 -Force | Out-Null }
    Copy-Item ./Packages/cn.etetet.wow/Bundles/Luban -Destination $dest4 -Recurse -Force

    $dest4 = "Publish/Packages/cn.etetet.startconfig/Bundles/"
    if (!(Test-Path $dest4)) { New-Item -ItemType Directory -Path $dest4 -Force | Out-Null }
    Copy-Item ./Packages/cn.etetet.startconfig/Bundles/Luban -Destination $dest4 -Recurse -Force
    pause
}

PublishLinux