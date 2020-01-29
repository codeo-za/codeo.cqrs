using System.Collections.Generic;
using System.Linq;
using Dapper;

namespace Codeo.CQRS
{
    /// <summary>
    /// https://github.com/StackExchange/dapper-dot-net/blob/master/Dapper.SqlBuilder/SqlBuilder.cs
    /// </summary>
    public class SqlBuilder
    {
        Dictionary<string, Clauses> _data = new Dictionary<string, Clauses>();
        int _seq;

        class Clause
        {
            public string Sql { get; set; }
            public object Parameters { get; set; }
            public bool IsInclusive { get; set; }
        }

        class Clauses : List<Clause>
        {
            private readonly string _joiner;
            private readonly string _prefix;
            private readonly string _postfix;

            public Clauses(string joiner, string prefix = "", string postfix = "")
            {
                _joiner = joiner;
                _prefix = prefix;
                _postfix = postfix;
            }

            public string ResolveClauses(DynamicParameters p)
            {
                foreach (var item in this)
                {
                    p.AddDynamicParams(item.Parameters);
                }

                return this.Any(a => a.IsInclusive)
                    ? _prefix +
                    string.Join(_joiner,
                        this.Where(a => !a.IsInclusive)
                            .Select(c => c.Sql)
                            .Union(new[]
                            {
                                " ( " +
                                string.Join(" OR ", this.Where(a => a.IsInclusive).Select(c => c.Sql).ToArray()) +
                                " ) "
                            })) + _postfix
                    : _prefix + string.Join(_joiner, this.Select(c => c.Sql)) + _postfix;
            }
        }

        public class Template
        {
            private readonly string _sql;
            private readonly SqlBuilder _builder;
            private readonly object _initParams;
            private int _dataSeq = -1; // Unresolved

            public Template(SqlBuilder builder, string sql, object parameters)
            {
                _initParams = parameters;
                _sql = sql;
                _builder = builder;
            }

            private static readonly System.Text.RegularExpressions.Regex FindMarkerRegex =
                new System.Text.RegularExpressions.Regex(@"\/\*\*.+\*\*\/",
                    System.Text.RegularExpressions.RegexOptions.Compiled |
                    System.Text.RegularExpressions.RegexOptions.Multiline);

            void ResolveSql()
            {
                if (_dataSeq != _builder._seq)
                {
                    var p = new DynamicParameters(_initParams);

                    _rawSql = _sql;

                    foreach (var pair in _builder._data)
                    {
                        _rawSql = _rawSql.Replace("/**" + pair.Key + "**/", pair.Value.ResolveClauses(p));
                    }

                    _parameters = p;

                    // replace all that is left with empty
                    _rawSql = FindMarkerRegex.Replace(_rawSql, "");

                    _dataSeq = _builder._seq;
                }
            }

            private string _rawSql;
            private DynamicParameters _parameters;

            public string RawSql
            {
                get
                {
                    ResolveSql();
                    return _rawSql;
                }
            }

            public DynamicParameters Parameters
            {
                get
                {
                    ResolveSql();
                    return _parameters;
                }
            }
        }


        public SqlBuilder()
        {
        }

        public Template AddTemplate(string sql, object parameters = null)
        {
            return new Template(this, sql, parameters);
        }

        SqlBuilder AddClause(string name,
            string sql,
            object parameters,
            string joiner,
            string prefix = "",
            string postfix = "",
            bool isInclusive = false)
        {
            Clauses clauses;
            if (!_data.TryGetValue(name, out clauses))
            {
                clauses = new Clauses(joiner, prefix, postfix);
                _data[name] = clauses;
            }

            clauses.Add(new Clause { Sql = sql, Parameters = parameters, IsInclusive = isInclusive });
            _seq++;
            return this;
        }

        public SqlBuilder Intersect(string sql, object parameters = null)
        {
            return AddClause("intersect", sql, parameters, joiner: "\nINTERSECT\n ", prefix: "\n ", postfix: "\n");
        }

        public SqlBuilder InnerJoin(string sql, object parameters = null)
        {
            return AddClause("innerjoin", sql, parameters, joiner: "\nINNER JOIN ", prefix: "\nINNER JOIN ",
                postfix: "\n");
        }

        public SqlBuilder LeftJoin(string sql, object parameters = null)
        {
            return AddClause("leftjoin", sql, parameters, joiner: "\nLEFT JOIN ", prefix: "\nLEFT JOIN ",
                postfix: "\n");
        }

        public SqlBuilder RightJoin(string sql, object parameters = null)
        {
            return AddClause("rightjoin", sql, parameters, joiner: "\nRIGHT JOIN ", prefix: "\nRIGHT JOIN ",
                postfix: "\n");
        }

        public SqlBuilder Where(string sql, object parameters = null)
        {
            return AddClause("where", sql, parameters, " AND ", prefix: "WHERE ", postfix: "\n");
        }

        public SqlBuilder OrWhere(string sql, object parameters = null)
        {
            return AddClause("where", sql, parameters, " AND ", prefix: "WHERE ", postfix: "\n", isInclusive: true);
        }

        public SqlBuilder OrderBy(string sql, object parameters = null)
        {
            return AddClause("orderby", sql, parameters, " , ", prefix: "ORDER BY ", postfix: "\n");
        }

        public SqlBuilder Select(string sql, object parameters = null)
        {
            return AddClause("select", sql, parameters, " , ", prefix: "", postfix: "\n");
        }

        public SqlBuilder AddParameters(object parameters)
        {
            return AddClause("--parameters", "", parameters, "");
        }

        public SqlBuilder Join(string sql, object parameters = null)
        {
            return AddClause("join", sql, parameters, joiner: "\nJOIN ", prefix: "\nJOIN ", postfix: "\n");
        }

        public SqlBuilder GroupBy(string sql, object parameters = null)
        {
            return AddClause("groupby", sql, parameters, joiner: " , ", prefix: "\nGROUP BY ", postfix: "\n");
        }

        public SqlBuilder Having(string sql, object parameters = null)
        {
            return AddClause("having", sql, parameters, joiner: "\nAND ", prefix: "HAVING ", postfix: "\n");
        }

        public SqlBuilder Limit(int limit)
        {
            return AddClause("limit", limit.ToString(), null, joiner: "\n", prefix: "LIMIT ", postfix: "\n");
        }

        public SqlBuilder NoLimit()
        {
            return AddClause("limit", "", null, joiner: "", prefix: "", postfix: "");
        }
    }
}