using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ivony.Web
{
  public static class HttpExtensions
  {

    public static Uri GetUrl( this HttpRequest request )
    {
      return new UriBuilder()
      {
        Scheme = request.Scheme,
        Host = request.Host.Host,
        Port = request.Host.Port.Value,
        Path = request.Path,
        Query = request.QueryString.Value
      }.Uri;
    }


    public static PathString ToDirectory( this PathString virtualPath )
    {
      if ( virtualPath.HasValue == false )
        return null;

      if ( virtualPath.Value.EndsWith( '/' ) )
        return virtualPath;


      return virtualPath + "/";
    }


    public static QueryString ToQueryString( this IQueryCollection query )
    {
      if ( query == null )
        throw new ArgumentNullException( nameof( query ) );


      var result = query
        .SelectMany( pair => pair.Value is StringValues values ? values.Select( v => new KeyValuePair<string, string>( pair.Key, v ) ) : new[] { new KeyValuePair<string, string>( pair.Key, pair.Value ) } )
        .OrderBy( pair => pair.Key ).ThenBy( pair => pair.Value )
        .Select( pair => $"{pair.Key}={pair.Value}" );


      if ( result.Any() == false )
        return new QueryString();

      return new QueryString( "?" + string.Join( "&", result ) );
    }



    public static string GetAppRelativePath( this HttpRequest request )
    {
      if ( request.Path.HasValue == false )
        return null;


      var path = request.Path.Value.Substring( request.PathBase.Value.Length );

      return "~/" + path.TrimStart( '/' );
    }

  }
}
