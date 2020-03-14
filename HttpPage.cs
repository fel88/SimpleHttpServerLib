namespace SimpleHttpServerLib
{
    public abstract class HttpPage
    {
        public virtual void OnPageLoad(SimpleHttpContext ctx) { }
        public SimpleHttpContext Context;
    }
}
