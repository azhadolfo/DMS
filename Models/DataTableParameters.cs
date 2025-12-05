namespace Document_Management.Models
{
    public class DataTablesParameters
    {
        public int Draw { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }
        public List<DataTablesColumn> Columns { get; set; } = [];
        public List<DataTablesOrder> Order { get; set; } = [];
        public DataTablesSearch Search { get; set; } = new();
    }

    public class DataTablesColumn
    {
        public string Data { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool Searchable { get; set; }
        public bool Orderable { get; set; }
        public DataTablesSearch Search { get; set; } = new();
    }

    public class DataTablesOrder
    {
        public int Column { get; set; }
        public string Dir { get; set; } = string.Empty;
    }

    public class DataTablesSearch
    {
        public string Value { get; set; } = string.Empty;
        public bool Regex { get; set; }
    }
}