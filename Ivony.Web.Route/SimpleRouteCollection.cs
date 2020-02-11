using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Ivony.Web
{
  public class SimpleRouteCollection : ISimpleRouteBuilder, IEnumerable<SimpleRouteRule>
  {


    private ICollection<SimpleRouteRule> _rules = new List<SimpleRouteRule>();


    /// <summary>
    /// 添加一个路由规则
    /// </summary>
    /// <param name="name">规则名称</param>
    /// <param name="urlPattern">URL 模式</param>
    /// <param name="routeValues">静态/默认路由值</param>
    /// <param name="queryKeys">可用于 QueryString 的参数，若为null则表示无限制</param>
    public virtual SimpleRouteRule AddRule( string name, string verb, bool oneway, string urlPattern, IDictionary<string, string> routeValues, IReadOnlyCollection<string> queryKeys )
    {
      if ( name == null )
        throw new ArgumentNullException( nameof( name ) );

      if ( urlPattern == null )
        throw new ArgumentNullException( nameof( urlPattern ) );

      if ( routeValues == null )
        throw new ArgumentNullException( nameof( routeValues ) );



      if ( urlPattern.StartsWith( "~/" ) == false )
      {
        if ( urlPattern.StartsWith( "/" ) )
          throw new ArgumentException( "urlPattern has invalid format", "urlPattern" );

        urlPattern = "~/" + urlPattern;
      }


      urlPattern = Regex.Replace( urlPattern, "/+", "/" );

      if ( urlPattern.Any() && urlPattern.EndsWith( "/" ) == false )
        urlPattern += "/";

      var rule = new SimpleRouteRule( name, urlPattern, verb, oneway, routeValues, queryKeys );

      return AddRule( rule );
    }

    /// <summary>
    /// 添加一个路由规则
    /// </summary>
    /// <param name="rule">路由规则</param>
    public virtual SimpleRouteRule AddRule( SimpleRouteRule rule )
    {


      if ( TryAddRule( rule, out var conflictRule ) == false )
      {
        if ( conflictRule.SimpleRouteTable != null )
          throw new InvalidOperationException( $"add rule \"{rule.Name}\" failed. there is a conflict rule \"{conflictRule.Name}\" in route table \"{conflictRule.SimpleRouteTable.Name}\"." );

        else
          throw new InvalidOperationException( $"add rule \"{rule.Name}\" failed. there is a conflict rule \"{conflictRule.Name}\" in route set." );
      }

      return rule;
    }



    /// <summary>
    /// 尝试添加一个路由规则
    /// </summary>
    /// <param name="rule">要添加的路由规则</param>
    /// <param name="conflictRule">与之冲突的路由规则（如果有的话）</param>
    /// <returns>是否添加成功</returns>
    public virtual bool TryAddRule( SimpleRouteRule rule, out SimpleRouteRule conflictRule )
    {

      if ( ConflictTable.TryAddRule( rule, out conflictRule ) )
      {
        _rules.Add( rule );
        return true;
      }

      else
        return false;
    }



    /// <summary>
    /// 用于检测路由冲突的路有冲突表
    /// </summary>
    protected virtual SimpleRouteConflictTable ConflictTable { get; } = new SimpleRouteConflictTable();



    ISimpleRouteBuilder ISimpleRouteBuilder.AddRule( string name, string verb, bool oneway, string urlPattern, IDictionary<string, string> routeValues, IReadOnlyCollection<string> queryKeys )
    {
      AddRule( name, verb, oneway, urlPattern, routeValues, queryKeys );
      return this;
    }

    IEnumerator<SimpleRouteRule> IEnumerable<SimpleRouteRule>.GetEnumerator()
    {
      return _rules.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return _rules.GetEnumerator();
    }
  }
}
