# Light App Specific Database

## Build-in Type System
Int32
Int64
Single
Double
String : Unicode UTF-16BE Only
Bool
DateTime
TimeSpan

## Attributes
Ignore
Index
PrimaryKey

## Database Structure
..\Folder
 - META - Metadata - 64KB
 - TBDF - Table Definition - 1MB
 - TSLG - Transaction Log - Up to 64MB
 - TBDT.0000 to TBDT.9999 - Database Data File - Up to 64MB per file
 - LOCK - Operation Lock
 - EXPD - Application-specific NV Storage - Key/Value Pair, int/size/data
 
 ## Table Definition Structure
 [BEGIN]TBDF[Padding 4byte]TBCL[Padding 4byte]<Title>[Padding 4byte]<Type>[Padding 4byte]<Attrib>[Padding 4byte]ENCL.....ENDF[END]
 
  
