# Introduction

As requirement changes, database needs changes. In order to render latest database change in ORM, migration generation is required. In this article, I will briefly discuss the general workflow of migration generation.

# Workflow

1.  Make sure you have changeset 1310 or higher checked out locally, which includes latest fix for database migration. After finishing database change in model definition file, switch to _EF Migration_ with any desired architecture **expect ARM/ARM64**.

2.  Build project Light (Universal Windows) and make sure build passed.

3.  Open Package Manager Console, set default project to Light.Managed and invoke the migration: `Add-Migration <Your Migration Name> -Context MedialibraryDbContext -StartupProject Light`

4.  Switch back to Debug configuration and increment migration level in file _Light.Managed/Constants/LibraryConstants.cs_.

5.  Start debugging and validate your migration. If migration passes verification, check it in.

# Notes

The configuration _EF Migration_ is a migration-specific configuration which disabled most WinRT-related library features, path hard-coded, which may cause unexpected behavior in application. Hence, do not attempt to run or debug any version with such build configuration.