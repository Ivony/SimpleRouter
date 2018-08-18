using System.Collections.Generic;

namespace Ivony.Web
{

  /// <summary>
  /// 定义简单路由表构建服务
  /// </summary>
  public interface ISimpleRouteBuilder
  {
    ISimpleRouteBuilder AddRule( string name, string verb, bool oneway, string urlPattern, IDictionary<string, string> routeValues, IReadOnlyCollection<string> queryKeys );
  }
}