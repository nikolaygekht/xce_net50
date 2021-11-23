#pragma unmanaged
#include "colorer/parserFactory.h"
#include "colorer/editor/baseEditor.h"

#pragma managed
#using <mscorlib.dll>
using namespace System::Runtime::InteropServices;
#include "ColorerFactory.h"
#include "SyntaxHighlighter.h"

namespace gehtsoft
{
namespace xce
{
namespace colorer
{

ColorerFactory::ColorerFactory()
{
    mFactory = 0;
}

ColorerFactory::~ColorerFactory()
{
    this->!ColorerFactory();
}

ColorerFactory::!ColorerFactory()
{
    if (mFactory != 0)
    {
        delete mFactory;
        mFactory = 0;
    }
}

void ColorerFactory::Init(System::String ^colorerCataloguePath, System::String ^colorerHRDClass, System::String ^colorerHRDName, int backParse)
{
    if (mFactory != 0)
        throw gcnew System::InvalidOperationException();
    if (colorerCataloguePath == nullptr)
        throw gcnew System::ArgumentNullException("colorerCataloguePath");
    if (colorerHRDClass == nullptr)
        throw gcnew System::ArgumentNullException("colorerHRDClass");
    if (colorerHRDName == nullptr)
        throw gcnew System::ArgumentNullException("colorerHRDName");

    mBackParse = backParse;

    System::IntPtr _path, _class, _name;
    ::DString *__path, *__class, *__name;
    _path = Marshal::StringToCoTaskMemUni(colorerCataloguePath);
    _class = Marshal::StringToCoTaskMemUni(colorerHRDClass);
    _name = Marshal::StringToCoTaskMemUni(colorerHRDName);

    __path = new ::DString((const wchar *)_path.ToPointer());
    __class = new ::DString((const wchar *)_class.ToPointer());
    __name = new ::DString((const wchar *)_name.ToPointer());

    try
    {
        mFactory = new ::ParserFactory(__path);
        mHRCParser = mFactory->getHRCParser();
        mRegionMapper = mFactory->createStyledMapper(__class, __name);
        if (mRegionMapper == 0)
        {
            delete __class;
            delete __name;
            __class = new ::DString("console");
            __name = new ::DString("default");
            mRegionMapper = mFactory->createStyledMapper(__class, __name);
        }
        if (mRegionMapper == 0)
            throw gcnew System::ArgumentException("The value of hdr class or hdr name is invalid");

    }
    catch (::Exception &e)
    {
        System::IntPtr msg((void *)const_cast<wchar *>(e.getMessage()->getWChars()));
        throw gcnew System::Exception(Marshal::PtrToStringUni(msg, e.getMessage()->length()));
    }
    finally
    {
        delete __path;
        delete __class;
        delete __name;

        Marshal::FreeCoTaskMem(_path);
        Marshal::FreeCoTaskMem(_class);
        Marshal::FreeCoTaskMem(_name);
    }
}

SyntaxRegion ^ColorerFactory::FindSyntaxRegion(System::String ^name)
{
    if (name == nullptr)
        throw gcnew System::ArgumentNullException("name");
    if (mFactory == 0)
        throw gcnew System::InvalidOperationException();

    System::IntPtr _name;
    ::DString *__name;

    _name = Marshal::StringToCoTaskMemUni(name);
    __name = new ::DString((const wchar *)_name.ToPointer());

    const ::Region *region = mHRCParser->getRegion(__name);

    delete __name;
    Marshal::FreeCoTaskMem(_name);

    if (region == 0)
        return nullptr;
    return gcnew SyntaxRegion(name, region);
}

StyledRegion ^ColorerFactory::FindStyledRegion(System::String ^name)
{
    if (name == nullptr)
        throw gcnew System::ArgumentNullException("name");
    if (mFactory == 0)
        throw gcnew System::InvalidOperationException();

    System::IntPtr _name;
    ::DString *__name;

    _name = Marshal::StringToCoTaskMemUni(name);
    __name = new ::DString((const wchar *)_name.ToPointer());

    const ::StyledRegion *region = ::StyledRegion::cast(mRegionMapper->getRegionDefine(*__name));

    delete __name;
    Marshal::FreeCoTaskMem(_name);

    if (region == 0)
        return nullptr;
    return gcnew StyledRegion(name, region);
}

SyntaxHighlighter ^ColorerFactory::CreateHighlighter(ILineSource ^lineSource)
{
    try
    {
        SyntaxHighlighter ^highlighter = gcnew SyntaxHighlighter(mFactory, mRegionMapper, lineSource, mBackParse);
        return highlighter;
    }
    catch (::Exception &e)
    {
        System::IntPtr msg((void *)const_cast<wchar *>(e.getMessage()->getWChars()));
        throw gcnew System::Exception(Marshal::PtrToStringUni(msg, e.getMessage()->length()));
    }

}


SyntaxRegion::SyntaxRegion(System::String ^name, const ::Region *region)
{
    mName = name;
    mRegion = region;
}

const ::Region *SyntaxRegion::Native()
{
    return mRegion;
}

System::String ^SyntaxRegion::Name::get()
{
    return mName;
}

bool SyntaxRegion::IsDerivedFrom(SyntaxRegion ^region)
{
    if (region == nullptr)
        throw gcnew System::ArgumentNullException("region");
    return mRegion->hasParent(region->Native());
}

bool SyntaxRegion::Equals(SyntaxRegion ^region)
{
    if (region == nullptr)
        return false;
    return mRegion->getID() == region->Native()->getID();
}

StyledRegion::StyledRegion(System::String ^name, const ::StyledRegion *region)
{
    mName = name;
    mRegion = region;
}

System::String ^StyledRegion::Name::get()
{
    return mName;
}

short StyledRegion::ConsoleColor::get()
{
    return (mRegion->cfore & 0xf) | ((mRegion->cback << 4) & 0xf0);
};

int StyledRegion::ForegroundColor::get()
{
    return mRegion->fore;
};

int StyledRegion::BackgroundColor::get()
{
    return mRegion->back;
};

int StyledRegion::Style::get()
{
    return mRegion->style;
};


}
}
}

