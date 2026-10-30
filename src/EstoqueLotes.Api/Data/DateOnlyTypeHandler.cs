using System.Data;
using Dapper;

namespace EstoqueLotes.Api.Data;

// O driver do SQL Server entrega colunas DATE como DateTime, e o Dapper não converte para DateOnly sozinho
// (o EF Core 8 converte; o Dapper, por ser "micro", não). Este handler ensina os dois sentidos:
//   Parse:    valor lido do banco -> DateOnly (preencher o DTO)
//   SetValue: DateOnly usado como @parâmetro -> parâmetro do tipo DATE
// Registrado uma vez no Program.cs com SqlMapper.AddTypeHandler.
public class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override DateOnly Parse(object value) => DateOnly.FromDateTime((DateTime)value);

    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value = value.ToDateTime(TimeOnly.MinValue);
    }
}
