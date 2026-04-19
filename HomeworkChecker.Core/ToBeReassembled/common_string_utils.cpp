//2452769 幸可函 计科
#include <iostream>
#include <iomanip>
#include "../include/common_string_utils.h"
using namespace std;

/***************************************************************************
  函数名称：csu_toLower
  功    能：将字符串转换为小写
  输入参数：const string& str - 待转换的字符串
  返 回 值：转换后的小写字符串
 ***************************************************************************/
string csu_toLower(const string& str)
{
	string ret = str;
	int len = ret.length();
	for (int i = 0; i < len; i++) {
		ret[i] = ccu_toLower(ret[i]);
	}
	return ret;
}

/***************************************************************************
  函数名称：csu_toUpper
  功    能：将字符串转换为大写
  输入参数：const string& str - 待转换的字符串
  返 回 值：转换后的大写字符串
 ***************************************************************************/
string csu_toUpper(const string& str)
{
	string ret = str;
	int len = ret.length();
	for (int i = 0; i < len; i++) {
		ret[i] = ccu_toUpper(ret[i]);
	}
	return ret;
}

/***************************************************************************
  函数名称：csu_StrtoIpaddr
  功    能：将字符串形式的IPv4地址转换为32位无符号整数表示
  输入参数：const string str_ip - 待转换的字符串形式的IPv4地址
  返 回 值：IpConversionResult结构体，包含转换结果和数值
  说    明：空串被当作正确的0.0.0.0
  ***************************************************************************/
IpConversionResult csu_StrtoIpaddr(const string str_ip)
{
	IpConversionResult result = { false, 0xffffffffU };//默认错误结果
	//空串被当作正确的0.0.0.0
	if (str_ip == "") {
		result.ICR_valid = true;
		result.ICR_value = 0U;
		return result;
	}
	//不是空串，开始转换
	istringstream IP_address(str_ip);
	const int ip_min = 0;
	const int ip_max = 255;
	int ip[4];
	for (int i = 0; i < 4; i++) {
		IP_address >> ip[i];
		//读入失败，返回错误结果
		if (IP_address.fail()) {
			return result;
		}
		//超限，返回错误结果
		if (ip[i] < ip_min || ip[i] > ip_max) {
			return result;
		}
		//检查间隔符号是否为.
		char ch = IP_address.peek();
		if (i < 3) {
			if (ch == '.') {
				IP_address.get();
			}
			else { //不是.，返回错误结果
				return result;
			}
		}
		else {//i==3，读完最后一个ip了
			if (!IP_address.eof()) {
				return result;
			}
		}
	}
	result.ICR_value = ((u_int)(ip[0]) << 24) + ((u_int)(ip[1]) << 16) + ((u_int)ip[2] << 8) + (u_int)(ip[3]);
	result.ICR_valid = true;
	return result;
}

/***************************************************************************
  函数名称：csu_trimLeft / csu_trimRight / csu_trimAll
  功    能：去掉字符串左侧/右侧/两侧的空格和tab（可选去掉CRLF）
  输入参数：string& str - 待裁剪的字符串引用
			bool ignore_crlf - 是否忽略CRLF，true表示忽略
  返 回 值：无，直接修改输入字符串
  说    明：
 ***************************************************************************/
void csu_trimLeft(string& str, bool ignore_crlf)
{
	int len = str.length();
	if (len == 0) //空串
	{
		return;
	}

	int start = 0; //裁剪后字符串的起始位置
	//去掉前面的空格和tab
	if (ignore_crlf)
	{
		while (start < len && (str[start] == ' ' || str[start] == '\t' || str[start] == '\r' || str[start] == '\n'))
		{
			start++;
		}
	}
	else
	{
		while (start < len && (str[start] == ' ' || str[start] == '\t'))
		{
			start++;
		}
	}

	if (start >= len) //全空白
	{
		str = "";
	}
	else
	{
		str = str.substr(start);
	}
}

void csu_trimRight(string& str, bool ignore_crlf)
{
	int len = str.length();
	if (len == 0) //空串
	{
		return;
	}

	int end = len - 1; //裁剪后字符串的结束位置
	//去掉后面的空格和tab
	if (ignore_crlf)
	{
		while (end >= 0 && (str[end] == ' ' || str[end] == '\t' || str[end] == '\r' || str[end] == '\n'))
		{
			end--;
		}
	}
	else
	{
		while (end >= 0 && (str[end] == ' ' || str[end] == '\t'))
		{
			end--;
		}
	}

	if (end < 0) //全空白
	{
		str = "";
	}
	else
	{
		str = str.substr(0, end + 1);
	}
}

void csu_trimAll(string& str, bool ignore_crlf)
{
	csu_trimLeft(str, ignore_crlf);
	csu_trimRight(str, ignore_crlf);
}

/***************************************************************************
  函数名称：csu_StrtoHexdump
  功    能：将字符串转换为hex dump格式
  输入参数：const string& str - 待转换的字符串
  返 回 值：转换后的hex dump格式字符串
  说    明：每行显示16个字节，格式为：
			地址: 十六进制字节(8个) - 十六进制字节(8个)  可打印字符
 ***************************************************************************/
string csu_StrtoHexdump(const string& str, EndType endtype)
{
	ostringstream output;
	string tmp_str = str;
	switch (endtype)
	{
		case END_CR:
			tmp_str += '\x0d'; // '\r'
			break;
		case END_LF:
			tmp_str += '\x0a'; // '\n'
			break;
		case END_CRLF:
			tmp_str += '\x0d'; // '\r'
			tmp_str += '\x0a'; // '\n'
			break;
		case END_EOF:
			tmp_str += "\x1a"; //代表EOF字符
			break;
		default:
			break;
	}
	int len = tmp_str.length();
	int offset = 0;

	while (offset < len) {
		char buffer[17] = { 0 };  //存储可打印字符部分

		//输出地址（8位十六进制，前导0）
		output << setfill('0') << setw(8) << hex << offset << " : ";

		//输出16个字节的十六进制值
		for (int i = 0; i < 16; i++) {
			if (offset + i < len) {
				unsigned char ch = (unsigned char)tmp_str[offset + i];
				//保存可打印字符
				buffer[i] = (ch >= 33 && ch <= 126) ? ch : '.';
				//输出十六进制值
				output << setw(2) << (unsigned int)ch << ' ';

				//在第8个字节后输出分隔符
				if (i == 7) {
					output << ((offset + i + 1 < len) ? "- " : "  ");
				}
			}
			else {
				//不足16字节时用空格填充
				output << "   ";
				if (i == 7) {
					output << "  ";
				}
			}
		}

		//输出可打印字符部分
		output << ' ' << buffer << endl;

		offset += 16;
	}

	return output.str();
}

/***************************************************************************
  函数名称：csu_CRLF_to_LF
  功    能：将字符串中的CRLF转换为LF
  输入参数：string& str - 待转换的字符串
  返 回 值：无
  说    明：直接修改输入字符串
 ***************************************************************************/
void csu_CRLF_to_LF(string& str)
{
	string result;
	int len = str.length();
	for (int i = 0; i < len; i++) {
		if (str[i] == '\r') {
			if (i + 1 < len && str[i + 1] == '\n') {
				//遇到CRLF，转换为LF
				result += '\n';
				i++; //跳过下一个LF
			}
			else {
				//单独的CR，保留
				result += '\r';
			}
		}
		else {
			result += str[i];
		}
	}
	str = result;
}

/***************************************************************************
  函数名称：csu_isDigitString
  功    能：检查字符串是否为纯数字串
  输入参数：const string& str - 待检查的字符串
  返 回 值：true - 是纯数字串，false - 否
  说    明：
 ***************************************************************************/
bool csu_isDigitString(const string& str)
{
	for (size_t i = 0; i < str.length(); i++) {
		if (!ccu_isDigit(str[i])) {
			return false;
		}
	}
	return true;
}