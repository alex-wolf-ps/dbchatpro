# Schema Filtering and Data Types Feature

This document describes the implementation of the schema filtering and data types display feature.

## Features Implemented

### 1. Data Types Display
- Column names now display with their data types in brackets: `column_name (data_type)`
- Examples:
  - `id (int)`
  - `username (VARCHAR(50))`
  - `price (DECIMAL(10,2))`

### 2. Schema Filtering
- **Table Name Filtering**: Search and filter tables by name
- **Column Name Filtering**: Search for tables containing specific column names
- **Case-insensitive search** for both filters
- **Real-time filtering** as you type

## Implementation Details

### Models Enhanced
1. **ColumnInfo** - New model to store column name and data type
2. **TableSchema** - Enhanced to use `ColumnInfo` while maintaining backward compatibility

### Database Services Updated
All database services now fetch column data types:
- **SqlServerDatabaseService** - Enhanced SQL query with proper data type formatting
- **PostgresDatabaseService** - Updated to include PostgreSQL-specific data type formatting
- **MySqlDatabaseService** - MySQL data type information included
- **OracleDatabaseService** - Oracle data type formatting implemented

### UI Components
- **Home.razor** - Added filtering input fields and filtering logic
- **ConnectDb.razor** - Automatically shows data types via backward compatibility

## Usage

### In the Schema Tab
1. **Browse Tables**: Expand table nodes to see columns with data types
2. **Filter by Table Name**: Type in the "Filter tables" field to find specific tables
3. **Filter by Column Name**: Type in the "Filter columns" field to find tables containing specific columns

### Backward Compatibility
- Existing code continues to work without changes
- The `Columns` property now returns formatted strings with data types
- New `ColumnInfos` property provides access to individual name and type components

## SQL Query Examples

### SQL Server
```sql
SELECT SCHEMA_NAME(schema_id) + '.' + o.Name AS 'TableName', 
       c.Name as 'ColumnName',
       CASE 
         WHEN t.name IN ('char', 'varchar', 'nchar', 'nvarchar') THEN t.name + '(' + CAST(c.max_length as VARCHAR) + ')'
         WHEN t.name IN ('decimal', 'numeric') THEN t.name + '(' + CAST(c.precision as VARCHAR) + ',' + CAST(c.scale as VARCHAR) + ')'
         ELSE t.name
       END as 'DataType'
FROM sys.columns c
JOIN sys.objects o ON o.object_id = c.object_id
JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE o.type = 'U'
ORDER BY o.Name, c.column_id
```

### PostgreSQL
```sql
SELECT table_name, 
       column_name,
       CASE 
         WHEN character_maximum_length IS NOT NULL THEN data_type || '(' || character_maximum_length::text || ')'
         WHEN numeric_precision IS NOT NULL AND numeric_scale IS NOT NULL THEN data_type || '(' || numeric_precision::text || ',' || numeric_scale::text || ')'
         ELSE data_type
       END as data_type_formatted
FROM information_schema.columns 
WHERE table_catalog = $1 AND table_schema = $2
ORDER BY table_name, ordinal_position
```