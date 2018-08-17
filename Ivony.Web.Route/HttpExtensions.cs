using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
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

  }
}
