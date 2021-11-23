
#include<unicode/EString.h>

EString::EString(void *data, estring_length_ptr length_acc, estring_char_ptr char_acc)
{
    mData = data;
    mLengthAcc = length_acc;
    mCharAcc = char_acc;
    mBaseIndex = 0;
    mLength = length_acc(data);
}

EString::EString(void *data, estring_length_ptr length_acc, estring_char_ptr char_acc, int baseindex, int length)
{
    mData = data;
    mLengthAcc = length_acc;
    mCharAcc = char_acc;
    mBaseIndex = baseindex;
    if (length < 0)
        mLength = length_acc(data) - baseindex;
    else
        mLength = length;
}

EString::~EString()
{
}

wchar EString::operator[](int i) const
{
    if (i < 0 || i >= mLength)
        throw StringIndexOutOfBoundsException(SString(i));
    return (wchar)mCharAcc(mData, mBaseIndex + i);
}

int EString::length() const
{
    return mLength;
}

/** Creates Dynamic string as substring of called object.
    @param s Starting string position.
    @param l Length of created string. If -1, creates string
           till end of current.
*/
String *EString::substring(int s, int l) const
{
    return new EString(mData, mLengthAcc, mCharAcc, mBaseIndex + s, l);
}

/* ***** BEGIN LICENSE BLOCK *****
 * Version: MPL 1.1/GPL 2.0/LGPL 2.1
 *
 * The contents of this file are subject to the Mozilla Public License Version
 * 1.1 (the "License"); you may not use this file except in compliance with
 * the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 *
 * Software distributed under the License is distributed on an "AS IS" basis,
 * WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License
 * for the specific language governing rights and limitations under the
 * License.
 *
 * The Original Code is the Colorer Library.
 *
 * The Initial Developer of the Original Code is
 * Cail Lomecb <cail@nm.ru>.
 * Portions created by the Initial Developer are Copyright (C) 1999-2003
 * the Initial Developer. All Rights Reserved.
 *
 * Contributor(s):
 *
 * Alternatively, the contents of this file may be used under the terms of
 * either the GNU General Public License Version 2 or later (the "GPL"), or
 * the GNU Lesser General Public License Version 2.1 or later (the "LGPL"),
 * in which case the provisions of the GPL or the LGPL are applicable instead
 * of those above. If you wish to allow use of your version of this file only
 * under the terms of either the GPL or the LGPL, and not to allow others to
 * use your version of this file under the terms of the MPL, indicate your
 * decision by deleting the provisions above and replace them with the notice
 * and other provisions required by the GPL or the LGPL. If you do not delete
 * the provisions above, a recipient may use your version of this file under
 * the terms of any one of the MPL, the GPL or the LGPL.
 *
 * ***** END LICENSE BLOCK ***** */
