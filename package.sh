#!/bin/bash
creator_name=everlaster
plugin_name=Lumination
package_version=$1
plugin_version=$2

usage()
{
    echo "Usage: ./package.sh <VarPackageVersion> <PluginVersion>"
    echo "e.g.   ./package.sh 1 1.0.0"
    exit 1
}

[ -z "$package_version" ] && usage
[ -z "$plugin_version" ] && usage

# Setup archive contents
publish_dir=publish/Custom/Scripts/$creator_name/$plugin_name
mkdir -p $publish_dir
cp meta.json publish/
cp *.cslist $publish_dir/
cp -r src $publish_dir/

# Additional packaging
subscene_dir=publish/Custom/SubScene/
mkdir -p $subscene_dir
cp -r $plugin_name/local/SubScene/* $subscene_dir/
rm $resource_dir/src/Manager.cs

# Update version info
sed -i "s/v0\.0\.0/v$plugin_version/g" publish/meta.json
sed -i "s/v0\.0\.0/v$plugin_version/g" $publish_dir/src/Lights.cs

# hide .cs files (plugin is loaded with .cslist)
for file in $(find $publish_dir -type f -name "*.cs"); do
    touch $file.hide
done

# Zip files to .var and cleanup
cd publish
package="$creator_name.$plugin_name.$package_version.var"
zip -r $package *
cd ..
mkdir -p ../../../../AddonPackages/Self
mv publish/$package ../../../../AddonPackages/Self
rm -rf publish

echo "Package $package created with version $plugin_version and moved to AddonPackages/Self."
