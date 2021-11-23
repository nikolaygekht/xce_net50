class ParserFactory;
class StyledHRDMapper;
class StyledRegion;
class HRCParser;
class Region;

namespace gehtsoft
{
namespace xce
{
namespace colorer
{

ref class SyntaxHighlighter;
interface class ILineSource;

public ref class SyntaxRegion : System::IEquatable<SyntaxRegion ^>
{
 private:
    const ::Region *mRegion;
    System::String ^mName;
 internal:
    SyntaxRegion(System::String ^name, const ::Region *region);
    const ::Region *Native();
 public:
    property System::String ^Name
    {
        System::String ^get();
    };


    bool IsDerivedFrom(SyntaxRegion ^region);

    virtual bool Equals(SyntaxRegion ^region);
};

public ref class StyledRegion
{
 private:
    const ::StyledRegion *mRegion;
    System::String ^mName;
 internal:
    StyledRegion(System::String ^name, const ::StyledRegion *region);
 public:
    property System::String ^Name
    {
        System::String ^get();
    };


    property short ConsoleColor
    {
        short get();
    };

    property int ForegroundColor
    {
        int get();
    };

    property int BackgroundColor
    {
        int get();
    };

    property int Style
    {
        int get();
    };
};

public ref class ColorerFactory
{
 private:
    ::ParserFactory *mFactory;
    ::StyledHRDMapper *mRegionMapper;
    ::HRCParser *mHRCParser;
    const ::StyledRegion *mDefaultRegion;
    int mBackParse;
 public:
    ColorerFactory();
    ~ColorerFactory();
    !ColorerFactory();

    void Init(System::String ^colorerCataloguePath, System::String ^colorerHRDClass, System::String ^colorerHRDName, int backParse);
    SyntaxHighlighter ^CreateHighlighter(ILineSource ^lineSource);
    SyntaxRegion ^FindSyntaxRegion(System::String ^name);
    StyledRegion ^FindStyledRegion(System::String ^name);
};

}
}
}
