using System;

namespace SimpleHttpServerLib
{
    public abstract class UrlRouter
    {
        //byte data adn mime-type
        public abstract Tuple<byte[], string> GetData(SimpleHttpRequest req);
    }


}
