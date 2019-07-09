using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Collections.Specialized;
using System.IO;
using System.Diagnostics;
using Microsoft.AspNetCore.Routing;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Ivony.Web
{

  /// <summary>
  /// 简单路由表，提供简单的路由服务
  /// </summary>
  public class SimpleRouteTable : SimpleRouteCollection, IRouter
  {


    /// <summary>
    /// 定义通过路由值获取虚拟路径的缓存键前缀。
    /// </summary>
    protected const string RouteValuesCacheKeyPrefix = "RouteValues_";
    /// <summary>
    /// 定义通过虚拟路径获取路由值的缓存键前缀。
    /// </summary>
    protected const string RouteUrlCacheKeyPrefix = "RouteVirtualPath_";




    /// <summary>
    /// 获取简单路由表实例名称
    /// </summary>
    public string Name
    {
      get;
      private set;
    }


    /// <summary>
    /// 创建一个简单路由表实例
    /// </summary>
    /// <param name="name">简单路由表名称</param>
    /// <param name="handler">处理路由请求的对象</param>
    /// <param name="mvcCompatible">是否产生MVC兼容的虚拟路径（去除~/）</param>
    public SimpleRouteTable( string name, IServiceProvider serviceProvider, IRouter defaultRouter = null, IRouteHandler handler = null, UrlEncoder encoder = null )
    {
      Name = name;
      Logger = serviceProvider.GetRequiredService<ILogger<SimpleRouteTable>>();
      Handler = handler;
      UrlEncoder = encoder ?? UrlEncoder.Default;
      DefaultRouter = defaultRouter;
    }








    private static readonly SimpleRouteConflictTable conflictTable = new SimpleRouteConflictTable();

    /// <summary>
    /// 重写 ConflictTable 属性使用全局冲突检测表
    /// </summary>
    protected override SimpleRouteConflictTable ConflictTable => conflictTable;




    /// <summary>
    /// 处理路由请求的对象
    /// </summary>
    public IRouteHandler Handler { get; }

    /// <summary>
    /// 用于记录日志的日志记录器
    /// </summary>
    public ILogger Logger { get; }

    /// <summary>
    /// 默认路由对象
    /// </summary>
    public IRouter DefaultRouter { get; }

    /// <summary>
    /// 获取 URL 默认编码格式
    /// </summary>
    public UrlEncoder UrlEncoder { get; }





    async Task IRouter.RouteAsync( RouteContext context )
    {

      var virtualPath = context.HttpContext.Request.GetAppRelativePath();

      var verb = context.HttpContext.Request.Method;
      var query = context.HttpContext.Request.Query;

      var routeData = GetRouteData( verb, PreprocessVirtualPath( virtualPath ), query );
      if ( routeData == null )
      {
        Logger.LogDebug( $"there is no route rule matched request: {verb} {virtualPath}{context.HttpContext.Request.QueryString}" );
        return;
      }
      Logger.LogDebug( $"route rule {routeData.DataTokens["RoutingRuleName"]} matched request: {verb} {virtualPath}{context.HttpContext.Request.QueryString}" );
      Logger.LogDebug( $"RouteData: {string.Join( ",", routeData.Values.Select( pair => string.Format( "\"{0}\" : \"{1}\"", pair.Key, pair.Value ) ).ToArray() )}" );

      context.RouteData = routeData;

      if ( Handler != null )
        context.Handler = Handler.GetRequestHandler( context.HttpContext, routeData );

      if ( DefaultRouter != null )
        await DefaultRouter.RouteAsync( context );
    }

    VirtualPathData IRouter.GetVirtualPath( VirtualPathContext context )
    {
      return GetVirtualPath( context );
    }


    protected static IMemoryCache Cache { get; } = new MemoryCache( new MemoryCacheOptions() );



    /// <summary>
    /// 获取请求的路由数据
    /// </summary>
    /// <param name="httpContext">HTTP 请求</param>
    /// <returns>路由数据</returns>
    public RouteData GetRouteData( string verb, string virtualPath, IQueryCollection query )
    {


      if ( IsIgnoredPath( virtualPath ) )
        return null;


      var cacheKey = RouteUrlCacheKeyPrefix + verb + "@" + virtualPath + query.ToQueryString();

      var routeData = Cache.Get<RouteData>( cacheKey );


      if ( routeData != null )
      {
        if ( routeData.Routers.Contains( this ) == false )
          return null;


        Logger.LogDebug( "route cache hitted." );

        return new RouteData( routeData );
      }

      Logger.LogDebug( "route cache missed." );




      var data = Rules
        .Where( r => r.Verb == null || r.Verb.Equals( verb, StringComparison.OrdinalIgnoreCase ) )
        .OrderBy( r => r.DynamicRouteKeys.Count )
        .Select( r => new
        {
          Rule = r,
          Values = r.GetRouteValues( virtualPath, query ),
        } )
        .Where( i => i.Values != null )
        .FirstOrDefault();

      if ( data == null )
        return null;


      routeData = new RouteData();

      foreach ( var pair in data.Values )
        routeData.Values.Add( pair.Key, pair.Value );

      foreach ( var pair in data.Rule.DataTokens )
        routeData.DataTokens.Add( pair.Key, pair.Value );

      routeData.DataTokens["RoutingRuleName"] = data.Rule.Name;
      routeData.Routers.Add( this );

      Cache.Set( cacheKey, routeData );


      return new RouteData( routeData );

    }

    public static string PreprocessVirtualPath( string virtualPath )
    {
      if ( virtualPath == null )
        throw new ArgumentNullException( nameof( virtualPath ) );


      var extension = Path.GetExtension( virtualPath );
      if ( extension != null && extension.Length > 0 )
        virtualPath = virtualPath.Remove( virtualPath.Length - extension.Length );

      if ( virtualPath.Any() && virtualPath.EndsWith( '/' ) == false )
        virtualPath = virtualPath + "/";

      return virtualPath;
    }



    /// <summary>
    /// 确定指定的虚拟路径是否应当是被忽略的
    /// </summary>
    /// <param name="virtualPath">要检查的虚拟路径</param>
    /// <returns>该虚拟路径是否应当被忽略</returns>
    protected virtual bool IsIgnoredPath( string virtualPath )
    {
      return false;
    }





    /// <summary>
    /// 尝试从路由值创建虚拟路径
    /// </summary>
    /// <param name="requestContext">当前请求上下文</param>
    /// <param name="values">路由值</param>
    /// <returns>虚拟路径信息</returns>
    public VirtualPathData GetVirtualPath( VirtualPathContext context )
    {

      var values = context.Values.ToDictionary( pair => pair.Key, pair => pair.Value == null ? null : pair.Value.ToString(), StringComparer.OrdinalIgnoreCase );

      if ( values.TryGetValue( "area", out var area ) && area == "" )
        values.Remove( "area" );

      var cacheKey = CreateCacheKey( values );

      var cachedItem = Cache.Get<Tuple<string, SimpleRouteRule>>( cacheKey );

      if ( cachedItem != null )
        return CreateVirtualPathData( context, cachedItem.Item1, cachedItem.Item2 );


      var keySet = new HashSet<string>( values.Keys, StringComparer.OrdinalIgnoreCase );


      var candidateRules = Rules
        .Where( r => !r.Oneway )                                               //不是单向路由规则
        .Where( r => keySet.IsSupersetOf( r.RouteKeys ) )                      //所有路由键都必须匹配
        .Where( r => keySet.IsSubsetOf( r.AllKeys ) || !r.LimitedQueries )     //所有路由键和查询字符串键必须能涵盖要设置的键。
        .Where( r => r.IsMatch( values ) )                                     //必须满足路由规则所定义的路由数据。
        .ToArray();

      if ( !candidateRules.Any() )
        return null;


      var bestRule = BestRule( candidateRules );

      var virtualPath = bestRule.CreateVirtualPath( values );


      if ( IsIgnoredPath( virtualPath ) )//如果产生的虚拟路径是被忽略的，则返回 null
      {
        Logger.LogWarning( $"the virtual path \"{virtualPath}\" generated by route rule <{Name}.{bestRule.Name}> is ignored." );
        return null;
      }


      Cache.Set( cacheKey, Tuple.Create( virtualPath, bestRule ), new MemoryCacheEntryOptions() { Priority = CacheItemPriority.High } );
      return CreateVirtualPathData( context, virtualPath, bestRule );
    }

    /// <summary>
    /// 创建 VirtualPathData 对象
    /// </summary>
    /// <param name="virtualPath">虚拟路径</param>
    /// <param name="rule">产生该虚拟路径的路由规则</param>
    /// <returns>VirtualPathData 对象</returns>
    protected VirtualPathData CreateVirtualPathData( VirtualPathContext context, string virtualPath, SimpleRouteRule rule )
    {

      virtualPath = Regex.Replace( virtualPath, "^~", context.HttpContext.Request.PathBase );

      var data = new VirtualPathData( this, virtualPath );

      foreach ( var pair in rule.DataTokens )
        data.DataTokens.Add( pair.Key, pair.Value );

      data.DataTokens["RoutingRuleName"] = rule.Name;

      return data;
    }

    /// <summary>
    /// 创建缓存键
    /// </summary>
    /// <param name="values">要创建缓存键的字典</param>
    /// <returns>创建的缓存键</returns>
    protected virtual string CreateCacheKey( Dictionary<string, string> values )
    {

      StringBuilder builder = new StringBuilder( RouteValuesCacheKeyPrefix );

      foreach ( var key in values.Keys )
      {
        var val = values[key];

        builder.Append( key.Replace( "\\", "\\\\" ).Replace( ":", "\\:" ).Replace( ";", "\\;" ) );
        builder.Append( ":" );

        if ( val == null )
          builder.Append( "@" );

        else
          builder.Append( val.Replace( "\\", "\\\\" ).Replace( ":", "\\:" ).Replace( ";", "\\;" ).Replace( "@", "\\@" ) );

        builder.Append( ";" );
      }

      return builder.ToString();
    }


    private SimpleRouteRule BestRule( IEnumerable<SimpleRouteRule> candidateRules )
    {

      //满足最多静态值的被优先考虑
      candidateRules = candidateRules.GroupBy( r => r.StaticRouteValues.Count ).OrderByDescending( group => group.Key ).First().ToArray();

      if ( candidateRules.Count() == 1 )
        return candidateRules.First();


      //拥有最多路由键的被优先考虑
      candidateRules = candidateRules.GroupBy( r => r.RouteKeys.Count ).OrderByDescending( group => group.Key ).First().ToArray();

      if ( candidateRules.Count() == 1 )
        return candidateRules.First();


      //拥有最少动态参数的被优先考虑
      candidateRules = candidateRules.GroupBy( p => p.DynamicRouteKeys.Count ).OrderBy( group => group.Key ).First().ToArray();

      if ( candidateRules.Count() == 1 )
        return candidateRules.First();


      //匹配 GET 请求的被优先考虑
      candidateRules = candidateRules.Where( r => r.Verb != null && r.Verb.Equals( "GET", StringComparison.OrdinalIgnoreCase ) ).ToArray();

      if ( candidateRules.Count() == 1 )
        return candidateRules.First();
      else
        return null;

    }



  }
}
