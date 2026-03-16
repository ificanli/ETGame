# et-excel - Excel 操作专家

这个 skill 专门负责 Excel 文件的读写、格式化、图表等操作。使用 CLI 模式调用 ET.ExcelMcp 工具。

## 使用场景

- 创建/读取 Excel 文件
- 单元格读写操作
- 批量数据导入导出
- 设置单元格样式和格式
- 添加图表、公式
- 数据筛选、排序
- 合并单元格
- 添加数据验证

## 命令行调用格式

```bash
dotnet Bin/ET.ExcelMcp.dll cli <工具名> '<JSON参数>'
```

### 查看所有可用工具

```bash
dotnet Bin/ET.ExcelMcp.dll cli list
```

### 查看某个工具的详细帮助

```bash
dotnet Bin/ET.ExcelMcp.dll cli help <工具名>
```

## 常用工具和示例

### 1. 文件操作 (excel_file_operations)

```bash
# 创建新 Excel 文件
dotnet Bin/ET.ExcelMcp.dll cli excel_file_operations '{"operation":"create","path":"output.xlsx"}'

# 转换为 CSV
dotnet Bin/ET.ExcelMcp.dll cli excel_file_operations '{"operation":"convert","inputPath":"input.xlsx","outputPath":"output.csv","format":"csv"}'
```

### 2. 单元格操作 (excel_cell)

```bash
# 写入单元格
dotnet Bin/ET.ExcelMcp.dll cli excel_cell '{"operation":"write","path":"test.xlsx","cell":"A1","value":"Hello"}'

# 读取单元格
dotnet Bin/ET.ExcelMcp.dll cli excel_cell '{"operation":"get","path":"test.xlsx","cell":"A1"}'

# 清空单元格
dotnet Bin/ET.ExcelMcp.dll cli excel_cell '{"operation":"clear","path":"test.xlsx","cell":"A1"}'
```

### 3. 批量数据操作 (excel_data_operations)

```bash
# 批量写入
dotnet Bin/ET.ExcelMcp.dll cli excel_data_operations '{"operation":"batch_write","path":"test.xlsx","data":[{"cell":"A1","value":"姓名"},{"cell":"B1","value":"年龄"}]}'

# 读取范围内容
dotnet Bin/ET.ExcelMcp.dll cli excel_data_operations '{"operation":"get_content","path":"test.xlsx","range":"A1:C10"}'

# 排序
dotnet Bin/ET.ExcelMcp.dll cli excel_data_operations '{"operation":"sort","path":"test.xlsx","range":"A1:C10","sortColumn":0,"ascending":true}'
```

### 4. 范围操作 (excel_range)

```bash
# 批量写入二维数据
dotnet Bin/ET.ExcelMcp.dll cli excel_range '{"operation":"write","path":"test.xlsx","range":"A1:B2","data":[["A","B"],["C","D"]]}'

# 复制范围
dotnet Bin/ET.ExcelMcp.dll cli excel_range '{"operation":"copy","path":"test.xlsx","sourceRange":"A1:B2","destRange":"D1"}'
```

### 5. 样式操作 (excel_style)

```bash
# 设置样式（加粗、背景色、字体颜色）
dotnet Bin/ET.ExcelMcp.dll cli excel_style '{"operation":"format","path":"test.xlsx","range":"A1:C1","bold":true,"backgroundColor":"#4472C4","fontColor":"#FFFFFF"}'

# 获取样式
dotnet Bin/ET.ExcelMcp.dll cli excel_style '{"operation":"get_format","path":"test.xlsx","range":"A1"}'
```

### 6. 公式操作 (excel_formula)

```bash
# 添加公式
dotnet Bin/ET.ExcelMcp.dll cli excel_formula '{"operation":"add","path":"test.xlsx","cell":"B5","formula":"=SUM(B1:B4)"}'

# 获取公式计算结果
dotnet Bin/ET.ExcelMcp.dll cli excel_formula '{"operation":"get_result","path":"test.xlsx","cell":"B5"}'
```

### 7. 图表操作 (excel_chart)

```bash
# 添加柱状图
dotnet Bin/ET.ExcelMcp.dll cli excel_chart '{"operation":"add","path":"test.xlsx","chartType":"Column","dataRange":"B1:B10","categoryAxisDataRange":"A1:A10","title":"销售数据"}'
```

### 8. 工作表操作 (excel_sheet)

```bash
# 创建新工作表
dotnet Bin/ET.ExcelMcp.dll cli excel_sheet '{"operation":"create","path":"test.xlsx","sheetName":"NewSheet"}'

# 列出所有工作表
dotnet Bin/ET.ExcelMcp.dll cli excel_sheet '{"operation":"list","path":"test.xlsx"}'

# 重命名工作表
dotnet Bin/ET.ExcelMcp.dll cli excel_sheet '{"operation":"rename","path":"test.xlsx","sheetIndex":0,"newName":"Summary"}'
```

### 9. 合并单元格 (excel_merge_cells)

```bash
# 合并单元格
dotnet Bin/ET.ExcelMcp.dll cli excel_merge_cells '{"operation":"merge","path":"test.xlsx","range":"A1:C1"}'

# 取消合并
dotnet Bin/ET.ExcelMcp.dll cli excel_merge_cells '{"operation":"unmerge","path":"test.xlsx","range":"A1:C1"}'
```

### 10. 行列操作 (excel_row_column)

```bash
# 插入行
dotnet Bin/ET.ExcelMcp.dll cli excel_row_column '{"operation":"insert_rows","path":"test.xlsx","startRow":2,"count":3}'

# 设置行高列宽
dotnet Bin/ET.ExcelMcp.dll cli excel_row_column '{"operation":"set_size","path":"test.xlsx","rowIndex":1,"rowHeight":30,"columnIndex":1,"columnWidth":20}'

# 自动调整列宽
dotnet Bin/ET.ExcelMcp.dll cli excel_row_column '{"operation":"auto_fit","path":"test.xlsx","columns":[1,2,3]}'
```

## 完整工具列表

| 工具名 | 功能 |
|--------|------|
| excel_file_operations | 文件创建、转换 |
| excel_cell | 单元格读写 |
| excel_data_operations | 批量数据、排序、统计 |
| excel_range | 范围操作、复制移动 |
| excel_style | 样式格式化 |
| excel_formula | 公式管理 |
| excel_chart | 图表操作 |
| excel_sheet | 工作表管理 |
| excel_merge_cells | 合并单元格 |
| excel_row_column | 行列插入删除 |
| excel_filter | 数据筛选 |
| excel_data_validation | 数据验证 |
| excel_conditional_formatting | 条件格式 |
| excel_hyperlink | 超链接 |
| excel_comment | 批注 |
| excel_image | 图片 |
| excel_named_range | 命名范围 |
| excel_pivot_table | 数据透视表 |
| excel_freeze_panes | 冻结窗格 |
| excel_group | 行列分组 |
| excel_protect | 保护设置 |
| excel_properties | 文档属性 |
| excel_print_settings | 打印设置 |
| excel_view_settings | 视图设置 |
| excel_get_cell_address | 地址转换 |

## 注意事项

1. **路径**：建议使用绝对路径，避免相对路径问题
2. **JSON 转义**：在 shell 中使用单引号包裹 JSON，内部使用双引号
3. **中文支持**：完全支持中文内容读写
4. **sheetIndex**：工作表索引从 0 开始
5. **outputPath**：写操作可指定输出路径，默认覆盖原文件

## 工作流程

当用户需要操作 Excel 时：

1. 确认操作类型（读/写/格式化/图表等）
2. 选择合适的工具
3. 构建 JSON 参数
4. 执行 CLI 命令
5. 检查输出结果
