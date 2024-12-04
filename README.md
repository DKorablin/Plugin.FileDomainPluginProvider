# File Domain Plugin Provider
This plugin uses almost same behavior as a Basic File Plugin Provider, but to make sure that DLL contains IPlugin interface, it's launching separate AppDomain, search for suitable classes and after that load it inside primary AppDomain.

This approach allows you to reduce the amount of assemblies loaded into memory from the file system and reduce the likelihood of collisions when loading many instances of the same assembly into the root AppDomain.

To pass different directories to current provider use SAL_Path command line argument and separate with «;» if you want to pass many folders with plugins. Is SAL_Path is not passed as a argument, then current directory where host is loaded is used to search for plugins.

After all plugins are loaded form target folder, file monitoring is applied and all new DLL's will be scanned for IPlugin instances and will be loaded in the runtime.

## Warning

Despite the fact that assemblies are checked in a separate domain, a collision with DLL Hell can occur at the time of creating a transparent proxy between 2 AppDomain's. So you need to make sure that there is no duplication of the current assembly in the plugin search folders.