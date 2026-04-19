//2452769 幸可函 计科
#pragma once
#include <string>
#include <sstream>
#include "../include/common_char_utils.h"
using namespace std;
typedef unsigned int u_int;

struct IpConversionResult {
	bool ICR_valid;
	u_int ICR_value;
};

enum EndType { END_NONE, END_EOF, END_CR, END_LF, END_CRLF };

const string EmptyMark = "<EMPTY>"; //用于标记字符串为空
/**
* @brief  将字符串转换为小写形式
* @param  待转换的字符串
* @return 转换后的字符串
 */
string csu_toLower(const string& str);

/**
* @brief  将字符串转换为大写形式
* @param  待转换的字符串
* @return 转换后的字符串
 */
string csu_toUpper(const string& str);

/**
* @brief  将字符串形式的IP地址转换为u_int32形式
* @param  待转换的字符串形式IP地址
* @return 转换后的结果IpConversionResult结构体
*/
IpConversionResult csu_StrtoIpaddr(const string str_ip);

/**
* @brief  去除字符串左侧的空格和tab
* @param  待处理的字符串（引用传递，会被修改）
*/
void csu_trimLeft(string& str, bool ignore_crlf);

/**
* @brief  去除字符串右侧的空格和tab
* @param  待处理的字符串（引用传递，会被修改）
*/
void csu_trimRight(string& str, bool ignore_crlf);

/**
* @brief  去除字符串两侧的空格和tab
* @param  待处理的字符串（引用传递，会被修改）
*/
void csu_trimAll(string& str, bool ignore_crlf = false);

/**
* @brief  将字符串转换为hex dump格式输出
* @param  str - 待转换的字符串
* @param  offset - 起始偏移地址（用于显示地址列）
* @return 转换后的hex dump格式字符串
* @note   格式：地址: 十六进制字节 - 十六进制字节  可打印字符
*         例如：00000000:  48 65 6C 6C 6F 20 57 6F - 72 6C 64 21 00 00 00 00  Hello World!....
*/
string csu_StrtoHexdump(const string& str, EndType endtype = END_NONE);

/**
* @brief  将字符串中的所有 CRLF 转换为 LF
* @param  待处理的字符串（引用传递，会被修改）
*/
void csu_CRLF_to_LF(string& str);

/**
* @brief  判断字符串是否全部由数字字符组成
* @param  str - 待判断的字符串
* @return 如果字符串非空且全部由数字字符组成，返回true；否则返回false
*/
bool csu_isDigitString(const string& str);