using Microsoft.AspNetCore.Http;
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


      var result = string.Join( "&", query.Select( pair => $"{pair.Key}={pair.Value}" ) );
      if ( result == "" )
        return new QueryString();

      else

        return new QueryString( "?" + result );
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
