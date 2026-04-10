using Document_Management.Data;
using Document_Management.Models;
using Microsoft.EntityFrameworkCore;

namespace Document_Management.Service
{
    public interface ILogQueryService
    {
        Task<DataTableResult<LogsModel>> GetActivityLogsAsync(DataTablesParameters parameters, CancellationToken cancellationToken);
    }

    public sealed class LogQueryService : ILogQueryService
    {
        private readonly ApplicationDbContext _dbContext;

        public LogQueryService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<DataTableResult<LogsModel>> GetActivityLogsAsync(DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            IQueryable<LogsModel> logsQuery = _dbContext.Logs
                .AsNoTracking()
                .OrderByDescending(log => log.Date);

            var totalRecords = await logsQuery.CountAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(parameters.Search.Value))
            {
                var searchValue = parameters.Search.Value.Trim();
                if (DateTime.TryParse(searchValue, out var searchDate))
                {
                    var startOfDay = searchDate.Date;
                    var endOfDay = startOfDay.AddDays(1);
                    logsQuery = logsQuery.Where(log =>
                        EF.Functions.ILike(log.Username, $"%{searchValue}%") ||
                        EF.Functions.ILike(log.Activity, $"%{searchValue}%") ||
                        (log.Date >= startOfDay && log.Date < endOfDay));
                }
                else
                {
                    logsQuery = logsQuery.Where(log =>
                        EF.Functions.ILike(log.Username, $"%{searchValue}%") ||
                        EF.Functions.ILike(log.Activity, $"%{searchValue}%"));
                }
            }

            var recordsFiltered = await logsQuery.CountAsync(cancellationToken);
            logsQuery = ApplySorting(logsQuery, parameters);

            var pageSize = parameters.Length <= 0 ? recordsFiltered : parameters.Length;
            var pagedData = await logsQuery
                .Skip(parameters.Start)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new DataTableResult<LogsModel>(parameters.Draw, totalRecords, recordsFiltered, pagedData);
        }

        private static IQueryable<LogsModel> ApplySorting(IQueryable<LogsModel> query, DataTablesParameters parameters)
        {
            if (parameters.Order.Count == 0 || parameters.Columns.Count == 0)
            {
                return query;
            }

            var order = parameters.Order[0];
            if (order.Column < 0 || order.Column >= parameters.Columns.Count)
            {
                return query;
            }

            var sortAscending = order.Dir.Equals("asc", StringComparison.OrdinalIgnoreCase);
            var columnName = parameters.Columns[order.Column].Data;

            return columnName switch
            {
                nameof(LogsModel.Id) => sortAscending
                    ? query.OrderBy(log => log.Id)
                    : query.OrderByDescending(log => log.Id),
                nameof(LogsModel.Username) => sortAscending
                    ? query.OrderBy(log => log.Username).ThenByDescending(log => log.Date)
                    : query.OrderByDescending(log => log.Username).ThenByDescending(log => log.Date),
                nameof(LogsModel.Activity) => sortAscending
                    ? query.OrderBy(log => log.Activity).ThenByDescending(log => log.Date)
                    : query.OrderByDescending(log => log.Activity).ThenByDescending(log => log.Date),
                nameof(LogsModel.Date) => sortAscending
                    ? query.OrderBy(log => log.Date)
                    : query.OrderByDescending(log => log.Date),
                _ => query
            };
        }
    }
}
