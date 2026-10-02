using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ExpenseHub.Api.Data;

internal sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(value => value.ToUniversalTime(), value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
    {
    }
}
