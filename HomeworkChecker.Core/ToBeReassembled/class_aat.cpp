/* 2452769 幸可函 计科 */
#define _CRT_SECURE_NO_WARNINGS
#include <iostream>
#include <sstream>
#include <iomanip>
#include <string>
#include "../include/class_aat.h"
//如有必要，可以加入其它头文件
#include <cstring>
#include <cmath>
#include <cstdlib>
#include "../include/common_char_utils.h"
#include "../include/common_string_utils.h"
using namespace std;

#if !ENABLE_LIB_COMMON_TOOLS //不使用lib才有效

/* ---------------------------------------------------------------
	 允许加入其它需要static函数（内部工具用）
   ---------------------------------------------------------------- */
static int get_intset_length(const int* set)
{
	int len = 0;
	if (set == NULL)
		return 0;
	while (set[len] != INVALID_INT_VALUE_OF_SET) {
		len++;
	}
	return len;
}

static int get_doubleset_length(const double* set)
{
	int len = 0;
	if (set == NULL)
		return 0;
	while (fabs(set[len] - INVALID_DOUBLE_VALUE_OF_SET) > DOUBLE_DELTA) {
		len++;
	}
	return len;
}

static int get_strset_length(const string* set)
{
	int len = 0;
	if (set == NULL)
		return 0;
	while (set[len] != "") {
		len++;
	}
	return len;
}

static int findArgIndex(const string cur_arg, args_analyse_tools* const args)
{
	for (int i = 0; args[i].get_name() != ""; i++) {
		if (args[i].get_name() == cur_arg)
			return i;//找到了
	}
	return -1;//没找到
}

static bool checkPrefix(const string arg, bool check_extarg = false)
{
	if (check_extarg) {
		//若是在检查extarg时调用，允许arg长度等于prefix_len
		if (arg.length() < PREFIX_LEN)
			return false;
	}
	else {
		//若是严格检查arg，则arg长度必须大于prefix_len
		if (arg.length() <= PREFIX_LEN)
			return false;
	}
	//length >= PREFIX_LEN / length > PREFIX_LEN
	for (int i = 0; i < PREFIX_LEN; i++) {
		if (arg[i] != ARG_PREFIX)
			return false;
	}
	return true;
}

static bool checkIsInt(const string& str)
{
	const char* p = str.c_str();
	//第一个位置允许出现正负号
	if (*p == '-' || *p == '+') {
		p++;
		//若之后没东西了，说明格式错误
		if (*p == '\0')
			return false;
	}
	//遍历str
	for (; *p; p++) {
		//是数字，检查通过，继续到下一个字符
		if (ccu_isDigit(*p)) {
			continue;
		}
		return false;
	}

	return true;
}

static bool checkIsDouble(const string& str)
{
	const char* p = str.c_str();
	bool has_dot = false;
	//第一个位置允许出现正负号
	if (*p == '-' || *p == '+') {
		p++;
		if (*p == '\0')
			return false;
	}
	//第一个位置不是正负号，是小数点，后面也必须跟东西
	else if(*p == '.') {
		p++;
		has_dot = true;
		if (*p == '\0')
			return false;
	}
	//遍历str
	for (; *p; p++) {
		//是数字，检查通过，继续到下一个字符
		if (ccu_isDigit(*p)) {
			continue;
		}
		//只允许出现一次小数点
		if (*p == '.') {
			if (has_dot)
				return false;
			has_dot = true;
			continue;
		}
		return false;
	}

	return true;
}



/***************************************************************************
  函数名称：
  功    能：
  输入参数：
  返 回 值：
  说    明：null
 ***************************************************************************/
args_analyse_tools::args_analyse_tools()
{
	init("", ST_EXTARGS_TYPE::null, 0);
}

/***************************************************************************
  函数名称：
  功    能：
  输入参数：
  返 回 值：
  说    明：boolean
 ***************************************************************************/
args_analyse_tools::args_analyse_tools(const char* name, const ST_EXTARGS_TYPE type, const int ext_num, const bool def)
{
	init(name, type, ext_num);
	extargs_bool_default = def;

}

/***************************************************************************
  函数名称：
  功    能：
  输入参数：
  返 回 值：
  说    明：int_with_default、int_with_error
 ***************************************************************************/
args_analyse_tools::args_analyse_tools(const char* name, const ST_EXTARGS_TYPE type, const int ext_num, const int def, const int _min, const int _max)
{
	init(name, type, ext_num);
	extargs_int_default = def;
	extargs_int_min = _min;
	extargs_int_max = _max;
	extargs_int_value = extargs_int_default;
}

/***************************************************************************
  函数名称：
  功    能：
  输入参数：
  返 回 值：
  说    明：int_with_set_default、int_with_set_error
 ***************************************************************************/
args_analyse_tools::args_analyse_tools(const char* name, const enum ST_EXTARGS_TYPE type, const int ext_num, const int def_of_set_pos, const int* const set)
{
	init(name, type, ext_num);
	//参数def_of_set_pos代表default为set中的第[def_of_set_pos]个值，如果此值超范围，缺省0
	int len = get_intset_length((int*)set);
	extargs_intset_length = len;
	extargs_int_default = (def_of_set_pos < 0 || def_of_set_pos >= len) ? set[0] : set[def_of_set_pos];
	extargs_int_set = new(nothrow) int[len];
	if(extargs_int_set == NULL) {
		cout << "分配int集合内存失败." << endl;
		exit(-1);
	}
	for (int i = 0; i < len; i++)
		extargs_int_set[i] = set[i];
	extargs_int_value = extargs_int_default;
}

/***************************************************************************
  函数名称：
  功    能：
  输入参数：
  返 回 值：
  说    明：str、ipaddr_with_default、ipaddr_with_error
 ***************************************************************************/
args_analyse_tools::args_analyse_tools(const char* name, const ST_EXTARGS_TYPE type, const int ext_num, const string def)
{
	init(name, type, ext_num);
	extargs_string_default = def;
	extargs_ipaddr_default = csu_StrtoIpaddr(def).ICR_value;
	extargs_string_value = extargs_string_default;
	extargs_ipaddr_value = extargs_ipaddr_default;
}

/***************************************************************************
  函数名称：
  功    能：
  输入参数：
  返 回 值：
  说    明：str_with_set_default、str_with_set_error
 ***************************************************************************/
args_analyse_tools::args_analyse_tools(const char* name, const ST_EXTARGS_TYPE type, const int ext_num, const int def_of_set_pos, const string* const set)
{
	init(name, type, ext_num);
	int len = get_strset_length((string*)set);
	extargs_stringset_length = len;
	extargs_string_default = (def_of_set_pos < 0 || def_of_set_pos >= len) ? set[0] : set[def_of_set_pos];
	extargs_string_set = new(nothrow) string[len];
	if(extargs_string_set == NULL) {
		cout << "分配string集合内存失败." << endl;
		exit(-1);
	}
	for (int i = 0; i < len; i++)
		extargs_string_set[i] = set[i];
	extargs_string_value = extargs_string_default;
}

/***************************************************************************
  函数名称：
  功    能：
  输入参数：
  返 回 值：
  说    明：double_with_default、double_with_error
 ***************************************************************************/
args_analyse_tools::args_analyse_tools(const char* name, const ST_EXTARGS_TYPE type, const int ext_num, const double	def, const double _min, const double _max)
{
	init(name, type, ext_num);
	extargs_double_default = def;
	extargs_double_min = _min;
	extargs_double_max = _max;
	extargs_double_value = extargs_double_default;
}

/***************************************************************************
  函数名称：
  功    能：
  输入参数：
  返 回 值：
  说    明：double_with_set_default、double_with_set_error
 ***************************************************************************/
args_analyse_tools::args_analyse_tools(const char* name, const enum ST_EXTARGS_TYPE type, const int ext_num, const int def_of_set_pos, const double* const set)
{
	init(name, type, ext_num);
	int len = get_doubleset_length((double*)set);
	extargs_doubleset_length = len;
	extargs_double_default = (def_of_set_pos < 0 || def_of_set_pos >= len) ? set[0] : set[def_of_set_pos];
	extargs_double_set = new(nothrow) double[len];
	if(extargs_double_set == NULL) {
		cout << "分配double集合内存失败." << endl;
		exit(-1);
	}
	for (int i = 0; i < len; i++)
		extargs_double_set[i] = set[i];
	extargs_double_value = extargs_double_default;
}

/***************************************************************************
  函数名称：
  功    能：
  输入参数：
  返 回 值：
  说    明：
 ***************************************************************************/
args_analyse_tools::~args_analyse_tools()
{
	if(extargs_int_set != NULL) {
		delete[] extargs_int_set;
		extargs_int_set = NULL;
	}
	if(extargs_double_set != NULL) {
		delete[] extargs_double_set;
		extargs_double_set = NULL;
	}
	if(extargs_string_set != NULL) {
		delete[] extargs_string_set;
		extargs_string_set = NULL;
	}
}

/* ---------------------------------------------------------------
	 允许AAT中自定义成员函数的实现（private）
   ---------------------------------------------------------------- */

/***************************************************************************
  函数名称：
  功    能：
  输入参数：
  返 回 值：
  说    明：初始化所有成员变量/常量
 ***************************************************************************/
void args_analyse_tools::init(const char* name, const enum ST_EXTARGS_TYPE type, const int ext_num)
{
	//常量初始化
	args_name = name;
	extargs_type = type;
	extargs_num = ext_num;

	extargs_bool_default = false;

	extargs_int_default = 0;
	extargs_int_min = 0;
	extargs_int_max = 0;
	extargs_int_set = NULL;

	extargs_double_default = 0.0;
	extargs_double_min = 0.0;
	extargs_double_max = 0.0;
	extargs_double_set = NULL;

	extargs_string_default = "";
	extargs_string_set = NULL;

	extargs_ipaddr_default = 0;
	//变量初始化
	args_existed = 0;
	extargs_int_value = 0;
	extargs_double_value = 0.0;
	extargs_string_value = "";
	extargs_ipaddr_value = 0;
}

/***************************************************************************
  函数名称：
  功    能：
  输入参数：
  返 回 值：
  说    明：打印附加参数缺失信息
 ***************************************************************************/
void args_analyse_tools::print_extarg_is_missing(const bool extarg_exist, const bool extarg_is_param) const
{
	ostringstream ostr;
	if (!extarg_exist)
		ostr << "参数[" << args_name << "]的附加参数不足. ";
	else if (extarg_is_param)
		ostr << "参数[" << args_name << "]缺少附加参数. ";
	cout << ostr.str();
	print_annotation();
	cout << endl;
}

/***************************************************************************
  函数名称：
  功    能：
  输入参数：
  返 回 值：
  说    明：打印注释信息（标注类型、范围、缺省）
 ***************************************************************************/
void args_analyse_tools::print_annotation() const
{
	ostringstream ostr;
	//根据类型打印注释
	switch (extargs_type) {
		case ST_EXTARGS_TYPE::int_with_default:
		case ST_EXTARGS_TYPE::int_with_error:
			ostr << "(类型:int, 范围[" << extargs_int_min << ".." << extargs_int_max << "]";
			//def:打印缺省值，error:不打印
			if (extargs_type == ST_EXTARGS_TYPE::int_with_default)
				ostr << " 缺省:" << extargs_int_default;
			ostr << ")";
			break;

		case ST_EXTARGS_TYPE::int_with_set_default:
		case ST_EXTARGS_TYPE::int_with_set_error:
			ostr << "(类型:int, 可取值[";
			for (int i = 0; i < extargs_intset_length; i++) {
				ostr << extargs_int_set[i];
				if (i != extargs_intset_length - 1)
					ostr << "/";
			}
			ostr << "]";
			//def:打印缺省值，error:不打印
			if (extargs_type == ST_EXTARGS_TYPE::int_with_set_default)
				ostr << " 缺省:" << extargs_int_default;
			ostr << ")";
			break;

		case ST_EXTARGS_TYPE::double_with_default:
		case ST_EXTARGS_TYPE::double_with_error:
			ostr << "(类型:double, 范围[" << extargs_double_min << ".." << extargs_double_max << "]";
			//def:打印缺省值，error:不打印
			if (extargs_type == ST_EXTARGS_TYPE::double_with_default)
				ostr << " 缺省:" << extargs_double_default;
			ostr << ")";
			break;

		case ST_EXTARGS_TYPE::double_with_set_default:
		case ST_EXTARGS_TYPE::double_with_set_error:
			ostr << "(类型:double, 可取值[";
			for (int i = 0; i < extargs_doubleset_length; i++) {
				ostr << extargs_double_set[i];
				if (i != extargs_doubleset_length - 1)
					ostr << "/";
			}
			ostr << "]";
			//def:打印缺省值，error:不打印
			if (extargs_type == ST_EXTARGS_TYPE::double_with_set_default)
				ostr << " 缺省:" << extargs_double_default;
			ostr << ")";
			break;

		case ST_EXTARGS_TYPE::str:
			ostr << "(类型:string";
			//def:打印缺省值，error:不打印。若无缺省值，则不打印
			if (extargs_string_default != "")
				ostr << " 缺省:" << extargs_string_default;
			ostr << ")";
			break;

		case ST_EXTARGS_TYPE::str_with_set_default:
		case ST_EXTARGS_TYPE::str_with_set_error:
			ostr << "(类型:string, 可取值[";
			for (int i = 0; i < extargs_stringset_length; i++) {
				ostr << extargs_string_set[i];
				if (i != extargs_stringset_length - 1)
					ostr << "/";
			}
			ostr << "]";
			//def:打印缺省值，error:不打印
			if (extargs_type == ST_EXTARGS_TYPE::str_with_set_default)
				ostr << " 缺省:" << extargs_string_default;
			ostr << ")";
			break;

		case ST_EXTARGS_TYPE::ipaddr_with_default:
		case ST_EXTARGS_TYPE::ipaddr_with_error:
			ostr << "(类型:IP地址";
			//def:打印缺省值，error:不打印。若无缺省值，则不打印
			if (extargs_type == ST_EXTARGS_TYPE::ipaddr_with_default) {
				ostr << " 缺省:" << ipdef_to_str();
			}
			ostr << ")";
			break;
	}
	cout << ostr.str();
}

/***************************************************************************
  函数名称：
  功    能：
  输入参数：
  返 回 值：
  说    明：将extargs_ipaddr_default转换为字符串形式
 ***************************************************************************/
string args_analyse_tools::ipdef_to_str() const
{
	u_int ipaddr_value = extargs_ipaddr_default;
	u_int addr_seg[4];
	ostringstream ostr;
	for (int i = 0; i < 4; i++) {
		addr_seg[3 - i] = ipaddr_value & 0xFF;
		ipaddr_value >>= 8;
	}
	ostr << addr_seg[0] << "." << addr_seg[1] << "." << addr_seg[2] << "." << addr_seg[3];
	return ostr.str();
}

/***************************************************************************
  函数名称：
  功    能：
  输入参数：
  返 回 值：
  说    明：调度解析附加参数,根据extargs_type调用相应的解析函数
 ***************************************************************************/
bool args_analyse_tools::parse_extarg(const string extarg)
{
	switch (extargs_type) {
			//int型
		case ST_EXTARGS_TYPE::int_with_default:
		case ST_EXTARGS_TYPE::int_with_error:
		case ST_EXTARGS_TYPE::int_with_set_default:
		case ST_EXTARGS_TYPE::int_with_set_error:
			return parse_int_extarg(extarg);
			break;
			//double型
		case ST_EXTARGS_TYPE::double_with_default:
		case ST_EXTARGS_TYPE::double_with_error:
		case ST_EXTARGS_TYPE::double_with_set_default:
		case ST_EXTARGS_TYPE::double_with_set_error:
			return parse_double_extarg(extarg);
			break;
			//string型（无需检查）
		case ST_EXTARGS_TYPE::str:
			extargs_string_value = extarg;
			return true;
			//string集合型
		case ST_EXTARGS_TYPE::str_with_set_default:
		case ST_EXTARGS_TYPE::str_with_set_error:
			return parse_strset_extarg(extarg);
			break;
			//IP地址型
		case ST_EXTARGS_TYPE::ipaddr_with_default:
		case ST_EXTARGS_TYPE::ipaddr_with_error:
			return parse_ipaddr_extarg(extarg);
			break;
		default:
			return false;
			break;
	}
}

/***************************************************************************
  函数名称：
  功    能：
  输入参数：
  返 回 值：
  说    明：解析int型附加参数
 ***************************************************************************/
bool args_analyse_tools::parse_int_extarg(const string extarg)
{
	int value = 0;
	//检查输入格式是否合法
	if (!checkIsInt(extarg)) {
		cout << "参数[" << args_name << "]的附加参数不是整数. ";
		print_annotation();
		cout << endl;
		return false;
	}
	/*格式正确*/
	value = atoi(extarg.c_str());
	//范围型
	if (extargs_type == ST_EXTARGS_TYPE::int_with_default || extargs_type == ST_EXTARGS_TYPE::int_with_error) {
		if (value < extargs_int_min || value > extargs_int_max) { //超出范围
			if (extargs_type == ST_EXTARGS_TYPE::int_with_default) {
				//def：忽略非法值，使用默认值
				extargs_int_value = extargs_int_default;
				return true;
			}
			else {
				//err：报错
				cout << "参数[" << args_name << "]的附加参数值(" << value << ")非法. ";
				print_annotation();
				cout << endl;
				return false;
			}
		}
		//没出错，赋值
		extargs_int_value = value;
		return true;
	}

	//set型
	if (extargs_type == ST_EXTARGS_TYPE::int_with_set_default || extargs_type == ST_EXTARGS_TYPE::int_with_set_error) {
		//寻找是否在集合中
		bool found = false;
		for (int i = 0; i < extargs_intset_length; i++) {
			if (value == extargs_int_set[i]) {
				found = true;
				break;
			}
		}

		if (!found) { //不在集合中
			if (extargs_type == ST_EXTARGS_TYPE::int_with_set_default) {
				//def：忽略非法值，使用默认值
				extargs_int_value = extargs_int_default;
				return true;
			}
			else {
				//err:报错
				cout << "参数[" << args_name << "]的附加参数值(" << value << ")非法. ";
				print_annotation();
				cout << endl;
				return false;
			}
		}
		//没出错，赋值
		extargs_int_value = value;
		return true;
	}

	return false;
}

/***************************************************************************
  函数名称：
  功    能：
  输入参数：
  返 回 值：
  说    明：解析double型附加参数
 ***************************************************************************/
bool args_analyse_tools::parse_double_extarg(const string extarg)
{
	double value = 0.0;
	//检查输入是否合法
	if (!checkIsDouble(extarg)) { 
		cout << "参数[" << args_name << "]的附加参数不是浮点数. ";
		print_annotation();
		cout << endl;
		return false;
	}
	/*格式正确*/
	value = atof(extarg.c_str());
	//范围型
	if (extargs_type == ST_EXTARGS_TYPE::double_with_default || extargs_type == ST_EXTARGS_TYPE::double_with_error) {
		if (value < extargs_double_min || value > extargs_double_max) { //超出范围
			if (extargs_type == ST_EXTARGS_TYPE::double_with_default) {
				//def：忽略非法值，使用默认值
				extargs_double_value = extargs_double_default;
				return true;
			}
			else {
				//err：报错
				cout << "参数[" << args_name << "]的附加参数值(" << value << ")非法. ";
				print_annotation();
				cout << endl;
				return false;
			}
		}
		//没出错，赋值
		extargs_double_value = value;
		return true;
	}

	//set型
	if (extargs_type == ST_EXTARGS_TYPE::double_with_set_default || extargs_type == ST_EXTARGS_TYPE::double_with_set_error) {
		//寻找是否在集合中
		bool found = false;
		for (int i = 0; i < extargs_doubleset_length; i++) {
			if (fabs(value - extargs_double_set[i]) <= DOUBLE_DELTA) {
				found = true;
				break;
			}
		}

		if (!found) { //没找到
			if (extargs_type == ST_EXTARGS_TYPE::double_with_set_default) {
				//def：忽略非法值，使用默认值
				extargs_double_value = extargs_double_default;
				return true;
			}
			else {
				//err:报错
				cout << "参数[" << args_name << "]的附加参数值(" << value << ")非法. ";
				print_annotation();
				cout << endl;
				return false;
			}
		}
		//没出错，赋值
		extargs_double_value = value;
		return true;
	}

	return false;
}

/***************************************************************************
  函数名称：
  功    能：
  输入参数：
  返 回 值：
  说    明：解析string集合型附加参数
 ***************************************************************************/
bool args_analyse_tools::parse_strset_extarg(const string extarg)
{
	//寻找是否在集合中
	bool found = false;
	for (int i = 0; i < extargs_stringset_length; i++) {
		if (extarg == extargs_string_set[i]) {
			found = true;
			break;
		}
	}

	if (!found) { //没找到
		if (extargs_type == ST_EXTARGS_TYPE::str_with_set_default) {
			//def：忽略非法值，使用默认值
			extargs_string_value = extargs_string_default;
			return true;
		}
		else {
			//err:报错
			cout << "参数[" << args_name << "]的附加参数值(" << extarg << ")非法. ";
			print_annotation();
			cout << endl;
			return false;
		}
	}
	//没出错，赋值
	extargs_string_value = extarg;
	return true;
}

/***************************************************************************
  函数名称：
  功    能：
  输入参数：
  返 回 值：
  说    明：解析IP地址型附加参数
 ***************************************************************************/
bool args_analyse_tools::parse_ipaddr_extarg(const string extarg)
{

	IpConversionResult result = csu_StrtoIpaddr(extarg);
	//用户不允许输入空串
	if (extarg == "")
		result.ICR_valid = false; 

	if (!result.ICR_valid) { //转换失败
		if (extargs_type == ST_EXTARGS_TYPE::ipaddr_with_default) {
			//def：忽略非法值，使用默认值
			extargs_ipaddr_value = extargs_ipaddr_default;
			return true;
		}
		else {
			//err:报错
			cout << "参数[" << args_name << "]的附加参数值(" << extarg << ")非法. ";
			print_annotation();
			cout << endl;
			return false;
		}
	}

	extargs_ipaddr_value = result.ICR_value;
	return true;
}

/***************************************************************************
  函数名称：
  功    能：
  输入参数：
  返 回 值：
  说    明：已实现，不要动
 ***************************************************************************/
const string args_analyse_tools::get_name() const
{
	return this->args_name;
}

/***************************************************************************
  函数名称：
  功    能：
  输入参数：
  返 回 值：
  说    明：加!!后，只能是0/1
			已实现，不要动
 ***************************************************************************/
const int args_analyse_tools::existed() const
{
	return !!this->args_existed;
}

/***************************************************************************
  函数名称：
  功    能：
  输入参数：
  返 回 值：
  说    明：已实现，不要动
 ***************************************************************************/
const int args_analyse_tools::get_int() const
{
	return this->extargs_int_value;
}

/***************************************************************************
  函数名称：
  功    能：
  输入参数：
  返 回 值：
  说    明：已实现，不要动
 ***************************************************************************/
const double args_analyse_tools::get_double() const
{
	return this->extargs_double_value;
}

/***************************************************************************
  函数名称：
  功    能：
  输入参数：
  返 回 值：
  说    明：已实现，不要动
 ***************************************************************************/
const string args_analyse_tools::get_string() const
{
	return this->extargs_string_value;
}

/***************************************************************************
  函数名称：
  功    能：
  输入参数：
  返 回 值：
  说    明：已实现，不要动
 ***************************************************************************/
const unsigned int args_analyse_tools::get_ipaddr() const
{
	return this->extargs_ipaddr_value;
}

/***************************************************************************
  函数名称：
  功    能：
  输入参数：
  返 回 值：
  说    明：将 extargs_ipaddr_value 的值从 0x7f000001 转为 "127.0.0.1"
 ***************************************************************************/
const string args_analyse_tools::get_str_ipaddr() const
{
	u_int ipaddr_value = extargs_ipaddr_value;
	u_int addr_seg[4];
	ostringstream ostr;
	for (int i = 0; i < 4; i++) {
		addr_seg[3 - i] = ipaddr_value & 0xFF;
		ipaddr_value >>= 8;
	}
	ostr << addr_seg[0] << "." << addr_seg[1] << "." << addr_seg[2] << "." << addr_seg[3];
	return ostr.str();
}


/***************************************************************************
  函数名称：
  功    能：
  输入参数：follow_up_args：是否有后续参数
			0  ：无后续参数
			1  ：有后续参数
  返 回 值：
  说    明：友元函数，解析命令行参数
***************************************************************************/
int args_analyse_process(const int argc, const char* const* const argv, args_analyse_tools* const args, const int follow_up_args)
{
	int i = 1;//i是下标
	/*函数返回值：-1 - 处理过程有错
					>0 - 可变参数处理完成后，当前停在argv[]的第几个下标位置，如果后面还有argv[]，则表示固定参数
	*/
	while (i < argc) {
		const string cur_arg = argv[i];
		bool prefix_valid = checkPrefix(cur_arg);
		
		//参数为空且无后续参数，跳过（若有后续参数，空串要被当作固定参数处理）
		if (follow_up_args == 0 && cur_arg == "") {
			i++;
			continue;
		}
		//判断前缀
		if (prefix_valid) {
			//前缀正确，查找参数表
			int index = findArgIndex(cur_arg, args);
			if (index == -1) {//没找到参数
				cout << "参数[" << cur_arg << "]非法." << endl;
				return -1;
			}
			//是合法参数

			//重复
			if (args[index].args_existed) {
				cout << "参数[" << cur_arg << "]重复." << endl;
				return -1;
			}
			args[index].args_existed = 1;//标记该参数已出现

			if (args[index].extargs_num == 1) {//有额外参数
				bool extarg_exist = true;
				bool extarg_is_param = false;
				i++;
				string extarg;
				//判断附加参数是否存在
				if (i >= argc) {//越界                
					extarg_exist = false;
				}
				else {
					extarg = argv[i];
					extarg_is_param = checkPrefix(extarg, true);//检查是否为"--"
				}
				//附加参数不存在
				if (!extarg_exist || extarg_is_param) {
					args[index].print_extarg_is_missing(extarg_exist, extarg_is_param);
					return -1;
				}
				//附加参数存在，进行处理
				bool extarg_valid = args[index].parse_extarg(extarg);
				//附加参数处理失败
				if (!extarg_valid) {
					return -1;
				}
			}
		}
		//前缀错误
		else {
			if (follow_up_args == 0) {
				//无后续参数，说明可变参数处理完毕，后面不允许有其它内容
				cout << "参数[" << cur_arg << "]格式非法(不是--开头的有效内容)." << endl;
				return -1;
			}
			else {
				//有后续参数，说明可变参数处理完毕，后面是固定参数
				break;
			}
		}
		i++;
	}
	return i;
}


/***************************************************************************
  函数名称：
  功    能：
  输入参数：
  返 回 值：
  说    明：友元函数，打印参数表
***************************************************************************/
int args_analyse_print(const args_analyse_tools* const args)
{
	/*args_analyse_print 函数输出参数表时，每行开头一个空格，每列宽度为列名/该列所有内
容的最大宽度，列之间一个空格，因此分隔线（===）需要动态确定 */
	const string col_name[PRINT_COL_COUNT] = { "name","type","default","exists","value","range/set" };
	int col_width[6] = { 0 };
	enum COL {
		COL_NAME = 0,
		COL_TYPE,
		COL_DEFAULT,
		COL_EXISTS,
		COL_VALUE,
		COL_RANGE_SET
	};
	for (int i = 0; i < PRINT_COL_COUNT; i++) {
		col_width[i] = col_name[i].length();
	}
	//计算每列宽度和aat个数
	int row_count = 0;
	while (args[row_count].get_name() != "")
		row_count++;

	string** table = new(nothrow) string * [row_count];
	if(table == NULL) {
		cout << "分配打印表内存失败." << endl;
		return -1;
	}

	for (int i = 0; i < row_count; i++) {
		table[i] = new(nothrow) string[PRINT_COL_COUNT];
		if(table[i] == NULL) {
			cout << "分配打印表内存失败." << endl;
			return -1;
		}

		//name列
		string name_str = args[i].get_name();
		table[i][COL_NAME] = name_str;
		col_width[COL_NAME] = max(col_width[COL_NAME], (int)(name_str.length()));

		//type列
		string type_str;
		switch (args[i].extargs_type) {
			case ST_EXTARGS_TYPE::boolean:
				type_str = "Bool";
				break;
			case ST_EXTARGS_TYPE::int_with_default:
				type_str = "IntWithDefault";
				break;
			case ST_EXTARGS_TYPE::int_with_error:
				type_str = "IntWithError";
				break;
			case ST_EXTARGS_TYPE::int_with_set_default:
				type_str = "IntSETWithDefault";
				break;
			case ST_EXTARGS_TYPE::int_with_set_error:
				type_str = "IntSETWithError";
				break;
			case ST_EXTARGS_TYPE::double_with_default:
				type_str = "DoubleWithDefault";
				break;
			case ST_EXTARGS_TYPE::double_with_error:
				type_str = "DoubleWithError";
				break;
			case ST_EXTARGS_TYPE::double_with_set_default:
				type_str = "DoubleSETWithDefault";
				break;
			case ST_EXTARGS_TYPE::double_with_set_error:
				type_str = "DoubleSETWithError";
				break;
			case ST_EXTARGS_TYPE::str:
				type_str = "String";
				break;
			case ST_EXTARGS_TYPE::str_with_set_default:
				type_str = "StringSETWithDefault";
				break;
			case ST_EXTARGS_TYPE::str_with_set_error:
				type_str = "StringSETWithError";
				break;
			case ST_EXTARGS_TYPE::ipaddr_with_default:
				type_str = "IPAddrWithDefault";
				break;
			case ST_EXTARGS_TYPE::ipaddr_with_error:
				type_str = "IPAddrWithError";
				break;
			default:
				type_str = "";
				break;
		}
		table[i][COL_TYPE] = type_str;
		col_width[COL_TYPE] = max(col_width[COL_TYPE], (int)(type_str.length()));

		//default列
		ostringstream def_ostr;
		def_ostr.setf(ios::fixed);
		switch (args[i].extargs_type) {
			case ST_EXTARGS_TYPE::boolean:
				def_ostr << (args[i].extargs_bool_default ? "true" : "false");
				break;
			case ST_EXTARGS_TYPE::int_with_default:
			case ST_EXTARGS_TYPE::int_with_set_default:
				def_ostr << args[i].extargs_int_default;
				break;
			case ST_EXTARGS_TYPE::double_with_default:
			case ST_EXTARGS_TYPE::double_with_set_default:
				def_ostr << args[i].extargs_double_default;
				break;
			case ST_EXTARGS_TYPE::str:
			case ST_EXTARGS_TYPE::str_with_set_default:
				def_ostr << ((args[i].extargs_string_default == "") ? "/" : args[i].extargs_string_default);
				break;
			case ST_EXTARGS_TYPE::ipaddr_with_default:
				def_ostr << args[i].ipdef_to_str();
				break;
			default: //error类型
				def_ostr << "/";
				break;
		}
		string def_str = def_ostr.str();
		table[i][COL_DEFAULT] = def_str;
		col_width[COL_DEFAULT] = max(col_width[COL_DEFAULT], (int)(def_str.length()));

		//exists列
		string exists_str = args[i].existed() ? "1" : "0";
		table[i][COL_EXISTS] = exists_str;
		col_width[COL_EXISTS] = max(col_width[COL_EXISTS], (int)(exists_str.length()));

		//value列
		ostringstream val_ostr;
		val_ostr.setf(ios::fixed);
		switch (args[i].extargs_type) {
			case ST_EXTARGS_TYPE::boolean:
				val_ostr << (args[i].existed() ? "true" : "/");
				break;
			case ST_EXTARGS_TYPE::int_with_default:
			case ST_EXTARGS_TYPE::int_with_error:
			case ST_EXTARGS_TYPE::int_with_set_default:
			case ST_EXTARGS_TYPE::int_with_set_error:
				if (args[i].existed())
					val_ostr << args[i].get_int();
				else
					val_ostr << "/";
				break;
			case ST_EXTARGS_TYPE::double_with_default:
			case ST_EXTARGS_TYPE::double_with_error:
			case ST_EXTARGS_TYPE::double_with_set_default:
			case ST_EXTARGS_TYPE::double_with_set_error:
				if (args[i].existed())
					val_ostr << args[i].get_double();
				else
					val_ostr << "/";
				break;
			case ST_EXTARGS_TYPE::str:
			case ST_EXTARGS_TYPE::str_with_set_default:
			case ST_EXTARGS_TYPE::str_with_set_error:
				if (args[i].existed())
					val_ostr << args[i].get_string();
				else
					val_ostr << "/";
				break;
			case ST_EXTARGS_TYPE::ipaddr_with_default:
			case ST_EXTARGS_TYPE::ipaddr_with_error:
				if (args[i].existed())
					val_ostr << args[i].get_str_ipaddr();
				else
					val_ostr << "/";
				break;
			default:
				val_ostr << "/";
				break;
		}
		string val_str = val_ostr.str();
		table[i][COL_VALUE] = val_str;
		col_width[COL_VALUE] = max(col_width[COL_VALUE], (int)(val_str.length()));

		//range/set列
		ostringstream range_set_ostr;
		range_set_ostr.setf(ios::fixed);
		switch (args[i].extargs_type) {
			case ST_EXTARGS_TYPE::int_with_default:
			case ST_EXTARGS_TYPE::int_with_error:
				range_set_ostr << "[" << args[i].extargs_int_min << ".." << args[i].extargs_int_max << "]";
				break;
			case ST_EXTARGS_TYPE::int_with_set_default:
			case ST_EXTARGS_TYPE::int_with_set_error:
				for (int j = 0; j < args[i].extargs_intset_length; j++) {
					range_set_ostr << args[i].extargs_int_set[j];
					if (j != args[i].extargs_intset_length - 1)
						range_set_ostr << "/";
				}
				break;
			case ST_EXTARGS_TYPE::double_with_default:
			case ST_EXTARGS_TYPE::double_with_error:
				range_set_ostr << "[" << args[i].extargs_double_min << ".." << args[i].extargs_double_max << "]";
				break;
			case ST_EXTARGS_TYPE::double_with_set_default:
			case ST_EXTARGS_TYPE::double_with_set_error:
				for (int j = 0; j < args[i].extargs_doubleset_length; j++) {
					range_set_ostr << args[i].extargs_double_set[j];
					if (j != args[i].extargs_doubleset_length - 1)
						range_set_ostr << "/";
				}
				break;
			case ST_EXTARGS_TYPE::str_with_set_default:
			case ST_EXTARGS_TYPE::str_with_set_error:
				for (int j = 0; j < args[i].extargs_stringset_length; j++) {
					range_set_ostr << args[i].extargs_string_set[j];
					if (j != args[i].extargs_stringset_length - 1)
						range_set_ostr << "/";
				}
				break;
			default:
				range_set_ostr << "/";
				break;
		}
		string range_set_str = range_set_ostr.str();
		table[i][COL_RANGE_SET] = range_set_str;
		col_width[COL_RANGE_SET] = max(col_width[COL_RANGE_SET], (int)(range_set_str.length()));

	}

	cout << "args_list:" << endl;
	cout.setf(ios::left);
	//打印分隔线

	cout << "=";
	for (int i = 0; i < PRINT_COL_COUNT; i++) {
		for (int j = 0; j < col_width[i] + 1; j++) {
			cout << "=";
		}
	}
	cout << endl;
	//打印列名
	cout << " ";
	for (int i = 0; i < PRINT_COL_COUNT; i++) {
		cout << setw(col_width[i]) << col_name[i] << " ";
	}
	cout << endl;
	//打印分隔线
	cout << "=";
	for (int i = 0; i < PRINT_COL_COUNT; i++) {
		for (int j = 0; j < col_width[i] + 1; j++) {
			cout << "=";
		}
	}
	cout << endl;
	//打印内容
	for (int i = 0; i < row_count; i++) {
		cout << " ";
		for (int j = 0; j < PRINT_COL_COUNT; j++) {
			cout << setw(col_width[j]) << table[i][j] << " ";
		}
		cout << endl;
	}
	//打印分隔线
	cout << "=";
	for (int i = 0; i <  PRINT_COL_COUNT; i++) {
		for (int j = 0; j < col_width[i] + 1; j++) {
			cout << "=";
		}
	}
	cout << endl << endl << endl;

	return 0;
}


#endif // !ENABLE_LIB_COMMON_TOOLS
