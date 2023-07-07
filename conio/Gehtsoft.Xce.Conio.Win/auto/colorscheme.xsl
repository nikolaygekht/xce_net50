<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet
    version="1.0"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
    <xsl:output method="text" encoding="Windows-1252"/>
    <xsl:template match="/" >
      //------------------------------------------------------------------------
      //This is auto-generated code. Do NOT modify it.
      //Modify ./auto/colorscheme.xml and ./auto/colorscheme.xslt instead!!!
      //------------------------------------------------------------------------
      #pragma warning disable RCS1036 // Remove redundant empty-line.
      #pragma warning disable RCS1037 // Remove trailing white-space.
      #pragma warning disable RCS1171 // Simplify lazy initialization.
      #pragma warning disable RCS1085 // Use auto-implemented property.

      using System;
      using System.Collections.Generic;
      using System.Text;
      using CanvasColor = Gehtsoft.Xce.Conio.Drawing.CanvasColor;

      namespace Gehtsoft.Xce.Conio.Win
      {
      public interface IColorScheme
      {
      <xsl:for-each select="./color-schemes/class[1]/group">
        #region <xsl:value-of select="./@doc" />
        <xsl:for-each select="./color">
		<xsl:if test="count(./@doc) > 0">
        ///&lt;summary&gt;
        ///<xsl:value-of select="./@doc" />
        ///&lt;/summary&gt;
		</xsl:if>
        CanvasColor <xsl:value-of select="../@name" /><xsl:value-of select="./@name" />
        {
            get;
        }
        </xsl:for-each>
        #endregion
        </xsl:for-each>
    }

    public class ColorScheme : IColorScheme
    {
        <xsl:for-each select="./color-schemes/class[1]/group">
        #region <xsl:value-of select="./@doc" />
        <xsl:for-each select="./color">
        private CanvasColor m<xsl:value-of select="../@name" /><xsl:value-of select="./@name" />;
		<xsl:if test="count(./@doc) > 0">
		///&lt;summary&gt;
		///<xsl:value-of select="./@doc" />
		///&lt;/summary&gt;
		</xsl:if>
        public CanvasColor <xsl:value-of select="../@name" /><xsl:value-of select="./@name" />
        {
            get
            {
                return m<xsl:value-of select="../@name" /><xsl:value-of select="./@name" />;
            }
            set
            {
                m<xsl:value-of select="../@name" /><xsl:value-of select="./@name" /> = value;
            }
        }
        </xsl:for-each>
        #endregion
        </xsl:for-each>

        public ColorScheme()
        {
        }
		
		public static IColorScheme Default => <xsl:value-of select="/color-schemes/class[@default='true'][1]/@name" />;

        <xsl:for-each select="/color-schemes/class">
			<xsl:variable name="class-name" select="./@name" />
        private static ColorScheme m<xsl:value-of select="$class-name"/> = null;

        public static IColorScheme <xsl:value-of select="$class-name"/>
        {
            get
            {
                if (m<xsl:value-of select="$class-name"/> == null)
                {
                    m<xsl:value-of select="$class-name"/> = new ColorScheme()
					{
                    <xsl:for-each select="./group">
                    <xsl:for-each select="./color">
                    <xsl:choose>
                    <xsl:when test="count(./@fg) > 0  and count(./@bg) > 0">
                    <xsl:value-of select="../@name" /><xsl:value-of select="./@name" /> = new CanvasColor(<xsl:value-of select="./@console" />, CanvasColor.RGB(<xsl:value-of select="./@fg" />), CanvasColor.RGB(<xsl:value-of select="./@bg" />)),
                    </xsl:when>
                    <xsl:otherwise>
                    <xsl:value-of select="../@name" /><xsl:value-of select="./@name" /> = new CanvasColor(<xsl:value-of select="./@console" />),
                    </xsl:otherwise>
                    </xsl:choose>
                    </xsl:for-each>
                    </xsl:for-each>
			        };
			}
			return m<xsl:value-of select="$class-name"/>;
			}
		}
		</xsl:for-each>
    }
}
    </xsl:template>
</xsl:stylesheet>

