using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ivony.Web
{
  /// <summary>
  /// 简单路由表冲突检查表
  /// </summary>
  public sealed class SimpleRouteConflictTable
  {

    private Dictionary<string, SimpleRouteRule> virtualPathList = new Dictionary<string, SimpleRouteRule>( StringComparer.OrdinalIgnoreCase );
    private Dictionary<string, SimpleRouteRule> routeValuesList = new Dictionary<string, SimpleRouteRule>( StringComparer.OrdinalIgnoreCase );


    private object _sync = new object();

    /// <summary>
    /// 尝试冲突检测表中添加一条记录，并检测与现有规则是否冲突，若有冲突则无法添加成功
    /// </summary>
    /// <param name="rule">要添加的规则</param>
    /// <param name="conflictRule">与之相冲突的规则，如果有的话</param>
    /// <returns>是否添加成功</returns>
    public bool TryAddRule( SimpleRouteRule rule, out SimpleRouteRule conflictRule )
    {

      var virtualPath = rule.GetVirtualPathDescriptor();
      var routeValues = rule.GetRouteValuesDescriptor();

      lock ( _sync )
      {

        conflictRule = GetConflict( virtualPath, routeValues );
        if ( conflictRule != null )
          return false;


        virtualPathList.Add( virtualPath, rule );
        routeValuesList.Add( routeValues, rule );
        return true;
      }
    }


    /// <summary>
    /// 获取可能与指定路由规则冲突的路由规则
    /// </summary>
    /// <param name="rule">要检查冲突的路由规则</param>
    /// <returns>可能与之冲突的路由规则</returns>
    public SimpleRouteRule GetConflictRule( SimpleRouteRule rule )
    {
      var virtualPath = rule.GetVirtualPathDescriptor();
      var routeValues = rule.GetRouteValuesDescriptor();

      return GetConflict( virtualPath, routeValues );
    }

    private SimpleRouteRule GetConflict( string virtualPathDescriptor, string routeValuesDescriptor )
    {

      SimpleRouteRule conflictRule;

      lock ( _sync )
      {
        if ( virtualPathList.TryGetValue( virtualPathDescriptor, out conflictRule ) )
          return conflictRule;

        if ( routeValuesList.TryGetValue( routeValuesDescriptor, out conflictRule ) )
          return conflictRule;
      }


      return conflictRule;

    }
  }
}
