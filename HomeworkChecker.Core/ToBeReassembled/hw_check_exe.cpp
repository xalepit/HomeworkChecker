/* 2452769 计科 幸可函 */
#define _CRT_SECURE_NO_WARNINGS
#include "hw_check_exe.h"
#include "../include_mariadb_x86/mysql/mysql.h"
#include <Windows.h>
using namespace std;

/*----------------辅助函数----------------------*/

/***************************************************************************
  函数名称：is_empty
  功    能：判断字符串是否为空或为<EMPTY>标记
  输入参数：s：待判断字符串
  返 回 值：true=空/标记空；false=非空
 ***************************************************************************/
static bool is_empty(const string& s)
{
	return s.empty() || s == EmptyMark;
}
/***************************************************************************
  函数名称：normalize_dir_with_backslash
  功    能：将目录字符串统一规范化为以'\\'结尾
  输入参数：dirname：目录字符串（引用，原地修改）
  返 回 值：无
 ***************************************************************************/
static void normalize_dir_with_backslash(string& dirname)
{
	if (dirname.empty()) //<EMPTY>也要加'\'
		return;

	if (dirname.back() != '\\')
		dirname.push_back('\\');
}

/***************************************************************************
  函数名称：force_to_default
  功    能：将越界的数值强制恢复为默认值
  输入参数：value：待检查的值（引用，可能被修改）
			default_value：默认值
			min_value：允许最小值
			max_value：允许最大值
  返 回 值：无
  说    明：用于处理配置文件中的int型参数
 ***************************************************************************/
static void force_to_default(int& value, const int default_value, const int min_value, const int max_value)
{
	if (value < min_value || value > max_value)
		value = default_value;
}

/***************************************************************************
  函数名称：file_exists
  功    能：判断文件是否存在（可打开）
  输入参数：path：文件路径
  返 回 值：true=存在；false=不存在或path为空
  说    明：使用ifstream尝试打开来判断；path为<EMPTY>视为不存在
 ***************************************************************************/
static bool file_exists(const string& path)
{
	if (::is_empty(path))
		return false;
	ifstream fin(path);
	return (bool)fin;
}

/***************************************************************************
  函数名称：directory_exists
  功    能：判断目录是否存在
  输入参数：path：目录路径
  返 回 值：true=存在且为目录；false=不存在或path为空
 ***************************************************************************/
static bool directory_exists(const string& path)
{
	if (::is_empty(path))
		return false;
	DWORD attrib = GetFileAttributesA(path.c_str());
	return (attrib != INVALID_FILE_ATTRIBUTES) && (attrib & FILE_ATTRIBUTE_DIRECTORY);
}

/***************************************************************************
  函数名称：read_string
  功    能：从配置文件按key读取字符串，未读到时按规则填充<EMPTY>
  输入参数：group：组名（含[]）
			Cfg_tool：配置文件工具对象
			key：项名
			value：输出值（引用）
			is_base_group：是否为最基层组（include最深的那一层）（控制参数缺省时的行为）
  返 回 值：无
  说    明：读到空串时写入<EMPTY>；基组未读到时也写<EMPTY>
 ***************************************************************************/
static void read_string(const string& group, config_file_tools& Cfg_tool, const string& key, string& value, bool is_base_group)
{
	if (Cfg_tool.item_get_null(group, key) == 1) {
		Cfg_tool.item_get_string(group, key, value);
		if (value.empty())
			value = EmptyMark;
		return;
	}
	if (is_base_group)
		value = EmptyMark;
}

/***************************************************************************
  函数名称：read_raw
  功    能：从配置文件按key读取原始字符串（不做空串转<EMPTY>）
  输入参数：group：组名（含[]）
			Cfg_tool：配置文件工具对象
			key：项名
			value：输出值（引用）
			is_base_group：是否为主组（控制参数缺省时的行为）
  返 回 值：无
  说    明：仅当基组未读到时才写<EMPTY>；读到空串会保留空串
 ***************************************************************************/
static void read_raw(const string& group, config_file_tools& Cfg_tool, const string& key, string& value, bool is_base_group)
{
	if (Cfg_tool.item_get_null(group, key) == 1) {
		Cfg_tool.item_get_raw(group, key, value);
		return;
	}

	if (is_base_group)
		value = EmptyMark;
}

/***************************************************************************
  函数名称：format_group_name
  功    能：将组名规范化为[xxx]形式
  输入参数：raw_name：组名（引用，原地修改）
  返 回 值：规范化后的组名
  说    明：空/标记空则直接返回；用于include/pipe数据组名匹配
 ***************************************************************************/
static string format_group_name(string& raw_name)
{
	if (::is_empty(raw_name)) {
		return raw_name;
	}
	if (raw_name.front() != '[') {
		raw_name = "[" + raw_name;
	}
	if (raw_name.back() != ']') {
		raw_name = raw_name + "]";
	}
	return raw_name;
}

/***************************************************************************
  函数名称：group_exists
  功    能：判断给定组名是否存在于组列表中
  输入参数：groups：组名列表
			group_name：目标组名
  返 回 值：true=存在；false=不存在
  说    明：用于校验配置组/pipe数据文件中的组是否存在
 ***************************************************************************/
static bool group_exists(const vector<string>& groups, const string& group_name)
{
	for (size_t i = 0; i < groups.size(); ++i) {
		if (groups[i] == group_name)
			return true;
	}
	return false;
}

/***************************************************************************
  函数名称：get_time_str
  功    能：获取当前本地时间字符串
  输入参数：无
  返 回 值："YYYY-MM-DD HH:MM:SS"格式字符串
  说    明：localtime失败返回"0000-00-00 00:00:00"
 ***************************************************************************/
static string get_time_str()
{
	time_t t = time(NULL);
	tm* lt = localtime(&t);
	if (!lt)
		return "0000-00-00 00:00:00";

	ostringstream oss;
	oss << (lt->tm_year + 1900) << "-";
	if (lt->tm_mon + 1 < 10)
		oss << "0";
	oss << (lt->tm_mon + 1) << "-";
	if (lt->tm_mday < 10)
		oss << "0";
	oss << lt->tm_mday << " ";

	if (lt->tm_hour < 10)
		oss << "0";
	oss << lt->tm_hour << ":";
	if (lt->tm_min < 10)
		oss << "0";
	oss << lt->tm_min << ":";
	if (lt->tm_sec < 10)
		oss << "0";
	oss << lt->tm_sec;

	return oss.str();
}

/***************************************************************************
  函数名称：format_time_for_xls
  功    能：将时间字符串转换为适合文件名/表格的形式
  输入参数：time_str："YYYY-MM-DD HH:MM:SS"
  返 回 值："YYYY-MM-DD-HH-MM-SS"
  说    明：将空格和冒号替换为'-'
 ***************************************************************************/
static string format_time_for_xls(const string& time_str)
{
	//"YYYY-MM-DD HH:MM:SS" -> "YYYY-MM-DD-HH-MM-SS"
	string r = time_str;
	for (size_t i = 0; i < r.size(); ++i) {
		if (r[i] == ' ' || r[i] == ':')
			r[i] = '-';
	}
	return r;
}

/***************************************************************************
  函数名称：slash_or_int
  功    能：将统计信息按“有无exe”输出为数字或'/'
  输入参数：enabled：是否有效
			num：数值
  返 回 值：enabled=true返回num字符串；否则返回"/"
  说    明：用于xls输出中无exe的学生用/占位
 ***************************************************************************/
static string slash_or_int(const bool enabled, const int num)
{
	return enabled ? to_string(num) : "/";
}


/***************************************************************************
  函数名称：quote_if_needed
  功    能：命令行参数含空格时自动加双引号
  输入参数：s：路径或参数
  返 回 值：必要时返回\"...\"，否则原样返回
  说    明：用于构造可被命令行正确解析的exe路径/参数
 ***************************************************************************/
static string quote_if_needed(const string& s)
{
	if (s.find(' ') != string::npos || s.find('\t') != string::npos) {
		return "\"" + s + "\"";
	}
	return s;
}

/***************************************************************************
  函数名称：basename_from_path
  功    能：从全路径中截取文件名部分
  输入参数：fullpath：全路径
  返 回 值：文件名（不含目录）
  说    明：同时兼容'\\'与'/'分隔符
 ***************************************************************************/
static string basename_from_path(const string& fullpath)
{
	const size_t pos = fullpath.find_last_of("\\/");
	if (pos == string::npos)
		return fullpath;
	return fullpath.substr(pos + 1);
}

/*----------------私有成员函数------------------------*/
/***************************************************************************
  函数名称：hw_checker::read_config_group
  功    能：读取指定组配置（支持include递归合并）
  输入参数：group：组名（含[]）
			config：输出配置对象（引用）
			Cfg_tool：配置文件工具对象
			groups：配置文件中所有组名列表
			is_base_group：是否为主组（控制缺省处理）
  返 回 值：无（通过checkcfg_ret/errors反映错误）
  说    明：递归include时先读被include组，再覆盖/补齐本组配置
 ***************************************************************************/
void hw_checker::read_config_group(const string& group, HW_GROUP_CFG& config, config_file_tools& Cfg_tool, vector<string> groups, bool is_base_group)
{
	//查找是否存在
	bool found = group_exists(groups, group);
	if (!found) {
		errors << "\n[--严重错误--] 配置文件[" << cfg_file << "]中的组[" << group << "]不存在/为空\n" << endl;
		checkcfg_ret = -1;
		return;
	}

	//include_name：存在且非空 -> 递归读取
	string include_name;
	Cfg_tool.item_get_string(group, "include", include_name);

	if (!include_name.empty()) {
		format_group_name(include_name);
		read_config_group(include_name, config, Cfg_tool, groups, true);
		if (checkcfg_ret != 0)
			return;
		is_base_group = false;
	}

	// ---------------- database ----------------
	//db_host：存在但为空 or 不存在 -> ""（允许）
	string db_group = "[数据库]";
	if (Cfg_tool.item_get_null(db_group, "db_host") == 1) {
		Cfg_tool.item_get_string(db_group, "db_host", config.database.db_host);
	}

	//db_port：不存在/为空 -> 默认3306；存在非3306 -> 允许
	if (Cfg_tool.item_get_null(db_group, "db_port") == 1) {
		string s;
		Cfg_tool.item_get_string(db_group, "db_port", s);
		if (s.empty()) {
			config.database.db_port = HW_DB_PORT_DEFAULT;
		}
		else {
			Cfg_tool.item_get_int(db_group, "db_port", config.database.db_port, INT_MIN, INT_MAX, HW_DB_PORT_DEFAULT);
		}
	}
	else {
		config.database.db_port = HW_DB_PORT_DEFAULT;
	}

	//db_name / db_username / db_passwd：item_get_string 读；读到空或没读到 -> <EMPTY>
	read_string(db_group, Cfg_tool, "db_name", config.database.db_name, is_base_group);
	read_string(db_group, Cfg_tool, "db_username", config.database.db_username, is_base_group);
	read_string(db_group, Cfg_tool, "db_passwd", config.database.db_passwd, is_base_group);
	read_string(db_group, Cfg_tool, "db_curr_term", config.database.db_curr_term, is_base_group);


	//db_cno_list：存在（可为空）OK；不存在 -> <EMPTY>
	if (Cfg_tool.item_get_null(db_group, "db_cno_list") == 1) {
		Cfg_tool.item_get_string(db_group, "db_cno_list", config.database.db_cno_list);
	}
	else {
		config.database.db_cno_list = EmptyMark;
	}

	// ---------------- name_list ----------------
	//缺省（没读到）=database；读到空（=后面没东西）视为“没读到”
	if (Cfg_tool.item_get_null(group, "name_list") == 1) {
		Cfg_tool.item_get_string(group, "name_list", config.name_list);
	}
	if (config.name_list.empty())
		config.name_list = "database";

	if (config.name_list == "database") {
		config.name_list_mode = HW_NAMELIST_MODE::database;
	}
	else {
		config.name_list_mode = HW_NAMELIST_MODE::file;
	}

	// ---------------- exe_style ----------------
	//exe_style项不存在或读到空 => 保持原值
	if (Cfg_tool.item_get_null(group, "exe_style") == 1) {
		Cfg_tool.item_get_string(group, "exe_style", config.exe_style_str);
		if (config.exe_style_str.empty())
			config.exe_style_str = "multi";
	}
	config.exe_style = parse_exe_style(config.exe_style_str);


	//exe 相关：
	read_string(group, Cfg_tool, "single_exe_dirname", config.single_exe_dirname, is_base_group);
	read_string(group, Cfg_tool, "multi_exe_main_dirname", config.multi_exe_main_dirname, is_base_group);
	read_string(group, Cfg_tool, "multi_exe_sub_dirname", config.multi_exe_sub_dirname, is_base_group);
	read_string(group, Cfg_tool, "stu_exe_name", config.stu_exe_name, is_base_group);
	//demo_exe_name：item_get_raw；只有没读到才置<EMPTY>；读到空串要保留空串
	read_raw(group, Cfg_tool, "demo_exe_name", config.demo_exe_name, is_base_group);

	//目录名读取最后统一处理成带 '\'
	normalize_dir_with_backslash(config.single_exe_dirname);
	normalize_dir_with_backslash(config.multi_exe_main_dirname);
	normalize_dir_with_backslash(config.multi_exe_sub_dirname);

	// ---------------- cmd_style ----------------
	//不存在/空 => 保持现值；否则照读，非法值留到 check_config 报错
	if (Cfg_tool.item_get_null(group, "cmd_style") == 1) {
		Cfg_tool.item_get_string(group, "cmd_style", config.cmd_style_str);
		if (config.cmd_style_str.empty())
			config.cmd_style_str = "normal";
	}
	config.cmd_style = parse_cmd_style(config.cmd_style_str);

	//cmd相关
	read_string(group, Cfg_tool, "pipe_get_input_data_exe_name", config.pipe_get_input_data_exe_name, is_base_group);
	read_string(group, Cfg_tool, "pipe_data_file", config.pipe_data_file, is_base_group);
	read_string(group, Cfg_tool, "redirection_data_dirname", config.redirection_data_dirname, is_base_group);
	normalize_dir_with_backslash(config.redirection_data_dirname);


	// ---------------- timeout/max_output_len ----------------
	//没读到或超限 => 默认值；不报错
	if (Cfg_tool.item_get_null(group, "timeout") == 1)
		Cfg_tool.item_get_int(group, "timeout", config.timeout);
	force_to_default(config.timeout, HW_TIMEOUT_DEFAULT, HW_TIMEOUT_MIN, HW_TIMEOUT_MAX);

	if (Cfg_tool.item_get_null(group, "max_output_len") == 1)
		Cfg_tool.item_get_int(group, "max_output_len", config.max_output_len);
	force_to_default(config.max_output_len, HW_MAX_OUTPUT_LEN_DEFAULT, HW_MAX_OUTPUT_LEN_MIN, HW_MAX_OUTPUT_LEN_MAX);

	//---------------- tc ----------------
	//tc_trim/tc_display：缺省分别 none；非法在 check_config 报错
	if (Cfg_tool.item_get_null(group, "tc_trim") == 1)
		Cfg_tool.item_get_string(group, "tc_trim", config.tc.tc_trim);
	if (Cfg_tool.item_get_null(group, "tc_display") == 1)
		Cfg_tool.item_get_string(group, "tc_display", config.tc.tc_display);

	//其它int参数：没读到或超限 => 默认值不报错（这里默认值用构造函数已有的 0）
	if (Cfg_tool.item_get_null(group, "tc_lineskip") == 1)
		Cfg_tool.item_get_int(group, "tc_lineskip", config.tc.tc_lineskip);
	if (Cfg_tool.item_get_null(group, "tc_lineoffset") == 1)
		Cfg_tool.item_get_int(group, "tc_lineoffset", config.tc.tc_lineoffset);
	if (Cfg_tool.item_get_null(group, "tc_ignoreblank") == 1)
		Cfg_tool.item_get_int(group, "tc_ignoreblank", config.tc.tc_ignoreblank);
	if (Cfg_tool.item_get_null(group, "tc_not_ignore_linefeed") == 1)
		Cfg_tool.item_get_int(group, "tc_not_ignore_linefeed", config.tc.tc_not_ignore_linefeed);
	if (Cfg_tool.item_get_null(group, "tc_maxdiff") == 1)
		Cfg_tool.item_get_int(group, "tc_maxdiff", config.tc.tc_maxdiff);
	if (Cfg_tool.item_get_null(group, "tc_maxline") == 1)
		Cfg_tool.item_get_int(group, "tc_maxline", config.tc.tc_maxline);

	force_to_default(config.tc.tc_lineskip, 0, 0, 100);
	force_to_default(config.tc.tc_lineoffset, 0, -100, 100);
	force_to_default(config.tc.tc_ignoreblank, 0, 0, 1);
	force_to_default(config.tc.tc_not_ignore_linefeed, 0, 0, 1);
	force_to_default(config.tc.tc_maxdiff, 0, 0, 100);
	force_to_default(config.tc.tc_maxline, 0, 0, 10000);



	//---------------- items ----------------
	//items_num/begin/end 缺省 0；超上下限要“报错并置缺省值”是在 check_config 做
	if (Cfg_tool.item_get_null(group, "items_num") == 1)
		Cfg_tool.item_get_int(group, "items_num", config.items.items_num);
	if (Cfg_tool.item_get_null(group, "items_begin") == 1)
		Cfg_tool.item_get_int(group, "items_begin", config.items.items_begin);
	if (Cfg_tool.item_get_null(group, "items_end") == 1)
		Cfg_tool.item_get_int(group, "items_end", config.items.items_end);


	const size_t n = (config.items.items_num > 0) ? (size_t)config.items.items_num : 0;

	const size_t old_n = (config.items.item_gname.size() > 0) ? (config.items.item_gname.size() - 1) : 0;

	config.items.item_gname.resize(n + 1);
	config.items.item_fname.resize(n + 1);
	config.items.item_args.resize(n + 1);

	for (size_t i = 1; i <= n - old_n; i++) {
		const size_t idx = old_n + i; //demo的行为是追加到尾部

		read_raw(group, Cfg_tool, "item_gname_" + to_string(i), config.items.item_gname[idx], is_base_group);
		read_raw(group, Cfg_tool, "item_fname_" + to_string(i), config.items.item_fname[idx], is_base_group);
		read_raw(group, Cfg_tool, "item_args_" + to_string(i), config.items.item_args[idx], is_base_group);
	}
	store_cfg_info(group);
	checkcfg_ret = 0;
}

/***************************************************************************
  函数名称：hw_checker::check_config
  功    能：检查配置项合法性，并记录错误信息
  输入参数：无（使用成员config）
  返 回 值：无（通过checkcfg_ret与errors反映结果）
  说    明：包括items范围、路径存在性、数据库配置、pipe/redirection数据检查等
 ***************************************************************************/
void hw_checker::check_config()
{

	// items_num/items_begin/items_end：范围错则报错并置缺省值；三者都错时不再报 > 关系错
	bool bad_num = false;
	bool bad_begin = false;
	bool bad_end = false;

	if (config.items.items_num < HW_ITEMS_MIN || config.items.items_num > HW_ITEMS_MAX)
		bad_num = true;
	if (config.items.items_begin < 1 || config.items.items_begin > HW_ITEMS_MAX)
		bad_begin = true;
	if (config.items.items_end < 1 || config.items.items_end > HW_ITEMS_MAX)
		bad_end = true;

	if (bad_num) {
		errors << "items_num[1]最小为1" << endl;
		config.items.items_num = 0;
		checkcfg_ret = -1;
	}
	if (bad_begin) {
		errors << "items_begin[1]最小为1" << endl;
		config.items.items_begin = 0;
		checkcfg_ret = -1;
	}
	if (bad_end) {
		errors << "items_end[1]最小为1" << endl;
		config.items.items_end = 0;
		checkcfg_ret = -1;
	}

	if (!(bad_num && bad_begin && bad_end)) {
		if (config.items.items_num > 0) {
			if (config.items.items_end > config.items.items_num) {
				errors << "items_end > items_num" << endl;
				checkcfg_ret = -1;
			}
			if (config.items.items_begin > 0 && config.items.items_end > 0) {
				if (config.items.items_begin > config.items.items_end) {
					errors << "items_begin > items_end" << endl;
					checkcfg_ret = -1;
				}
			}
		}
	}

	//exe_style分支
	switch (config.exe_style) {
		case HW_EXE_STYLE::none:
			if (!file_exists(config.demo_exe_name)) {
				errors << "demo_exe_name 指定的文件[" << config.demo_exe_name << "]不存在." << endl;
				checkcfg_ret = -1;
			}
			break;

		case HW_EXE_STYLE::multi:
			if (!directory_exists(config.multi_exe_main_dirname)) {
				errors << "multi_exe_main_dirname 指定的目录[" << config.multi_exe_main_dirname << "]不存在." << endl;
				checkcfg_ret = -1;
			}
			if (!file_exists(config.demo_exe_name)) {
				errors << "demo_exe_name 指定的文件[" << config.demo_exe_name << "]不存在." << endl;
				checkcfg_ret = -1;
			}
			break;

		case HW_EXE_STYLE::single:
			if (!directory_exists(config.single_exe_dirname)) {
				errors << "single_exe_dirname 指定的目录[" << config.single_exe_dirname << "]不存在." << endl;
				checkcfg_ret = -1;
			}
			if (!file_exists(config.demo_exe_name)) {
				errors << "demo_exe_name 指定的文件[" << config.demo_exe_name << "]不存在." << endl;
				checkcfg_ret = -1;
			}
			break;

		case HW_EXE_STYLE::invalid:
			errors << "exe_style的值不是none/single/multi" << endl;
			checkcfg_ret = -1;
		default:
			break;
	}

	//cmd_style分支检查 items
	switch (config.cmd_style) {
		case HW_CMD_STYLE::pipe:
		{
			// 前提：pipe_data_file 只要文件不存在则后续都不检查
			if (!file_exists(config.pipe_data_file)) {
				errors << "pipe_data_file 指定的文件[" << config.pipe_data_file << "]不存在." << endl;
				checkcfg_ret = -1;
				break;
			}

			//文件存在：逐个检查 item_gname_i
			config_file_tools t(config.pipe_data_file, BREAK_CTYPE::Space);

			vector<string> groups;
			t.get_all_group(groups);
			for (int i = 1; i <= config.items.items_num; ++i) {
				const string key = "item_gname_" + to_string(i);
				string g = config.items.item_gname[i];

				//项不存在（没读到）
				if (g == EmptyMark) {
					errors << "pipe方式的配置项[" << key << "]不存在" << endl;
					checkcfg_ret = -1;
					continue;
				}

				//项存在（读到了），但 pipe_data_file 中没有这个组

				format_group_name(g);
				if (!group_exists(groups, g)) {
					errors << "pipe_data_file 指定的文件[" << config.pipe_data_file << "]中没有组[" << g << "]." << endl;
					checkcfg_ret = -1;
				}
			}
			break;
		}
		case HW_CMD_STYLE::redirection:

			// redirection_data_dirname：存在但目录不存在：先报（后续fname仍继续检查）
			if (!directory_exists(config.redirection_data_dirname)) {
				errors << "redirection_data_dirname 指定的目录[" << config.redirection_data_dirname << "]不存在." << endl;
				checkcfg_ret = -1;
			}

			for (int i = 1; i <= config.items.items_num; ++i) {
				const string key = "item_fname_" + to_string(i);
				const string& testfile = config.items.item_fname[i];

				//项不存在：优先报这一条（不再做路径存在性检查）
				if (testfile == EmptyMark) {
					errors << "redirection方式的配置项[" << key << "]不存在" << endl;
					checkcfg_ret = -1;
					continue;
				}

				const string fullpath = config.redirection_data_dirname + testfile;
				if (!file_exists(fullpath)) {
					errors << "redirection方式：数据文件[" << fullpath << "]不存在." << endl;
					checkcfg_ret = -1;
				}
			}
			break;


		case HW_CMD_STYLE::main_with_arguments:
			for (int i = 1; i <= config.items.items_num; ++i) {
				const string key = "item_args_" + to_string(i);
				const string& a = config.items.item_args[i];

				if (a == EmptyMark) {
					errors << "main_with_arguments方式的配置项[" << key << "]不存在" << endl;
					checkcfg_ret = -1;
				}
			}
			break;

		case HW_CMD_STYLE::invalid:
			errors << "cmd_style的值不是normal/pipe/redirection/main_with_arguments" << endl;
			checkcfg_ret = -1;
		case HW_CMD_STYLE::normal:
		default:
			break;
	}

	//tc_trim/tc_display：非法值报错
	if (!is_valid_trim(config.tc.tc_trim)) {
		errors << "tc_trim的值不是none/left/right/all" << endl;
		checkcfg_ret = -1;
	}
	if (!is_valid_display(config.tc.tc_display)) {
		errors << "tc_display的值不是none/normal/detailed" << endl;
		checkcfg_ret = -1;
	}
	//name_list：非database => 文件不存在报错；database模式则不需要这个检查
	if (config.name_list_mode == HW_NAMELIST_MODE::file) {
		if (!file_exists(config.name_list)) {
			errors << "name_list 指定的文件[" << config.name_list << "]不存在." << endl;
			checkcfg_ret = -1;
		}
	}

	//database相关错误：仅当 name_list_mode==database 才检查“[数据库]错误”
	if (config.name_list_mode == HW_NAMELIST_MODE::database) {
		if (config.database.db_name == EmptyMark) {
			errors << "[数据库]的 db_name 配置项不存在" << endl;
			checkcfg_ret = -1;
		}
		if (config.database.db_username == EmptyMark) {
			errors << "[数据库]的 db_username 配置项不存在" << endl;
			checkcfg_ret = -1;
		}
		if (config.database.db_passwd == EmptyMark) {
			errors << "[数据库]的 db_passwd 配置项不存在" << endl;
			checkcfg_ret = -1;
		}
		if (config.database.db_cno_list == EmptyMark) {
			errors << "[数据库]的 db_cno_list 配置项不存在" << endl;
			checkcfg_ret = -1;
		}

	}

}

/***************************************************************************
  函数名称：hw_checker::store_cfg_info
  功    能：生成配置摘要信息，写入messages流中
  输入参数：group：当前检查组名（含[]）
  返 回 值：无
  说    明：按exe_style/cmd_style动态选择需要打印的配置项
 ***************************************************************************/
void hw_checker::store_cfg_info(const string& group)
{
	cfg_used.clear();
	messages << left << endl; //左对齐

	string show_name = group;
	if (!show_name.empty() && show_name.front() == '[' && show_name.back() == ']')
		show_name = show_name.substr(1, show_name.size() - 2);

	//-----------先决定要打印的配置项-----------
	//name_list相关
	if (config.exe_style != HW_EXE_STYLE::none && config.name_list_mode == HW_NAMELIST_MODE::database) {
		cfg_used.push_back("db_host");
		cfg_used.push_back("db_port");
		cfg_used.push_back("db_name");
		cfg_used.push_back("db_username");
		/*cfg_used.push_back("db_passwd");*/ //密码不打印
		cfg_used.push_back("db_curr_term");
		cfg_used.push_back("db_cno_list");
	}

	cfg_used.push_back("exe_style");
	//只在exe_style != none时打印name_list
	if (config.exe_style != HW_EXE_STYLE::none)
		cfg_used.push_back("name_list");
	//exe_style相关
	switch (config.exe_style) {
		case HW_EXE_STYLE::single:
			cfg_used.push_back("single_exe_dirname");
			cfg_used.push_back("stu_exe_name");
			cfg_used.push_back("demo_exe_name");
			break;
		case HW_EXE_STYLE::invalid: //exe_style非法时按multi打印
		case HW_EXE_STYLE::multi:
			cfg_used.push_back("multi_exe_main_dirname");
			cfg_used.push_back("multi_exe_sub_dirname");
			cfg_used.push_back("stu_exe_name");
			cfg_used.push_back("demo_exe_name");
			break;
		case HW_EXE_STYLE::none:
			cfg_used.push_back("demo_exe_name");
			break;
		default:
			break;
	}

	cfg_used.push_back("cmd_style");
	//通用项
	cfg_used.push_back("max_output_len");
	cfg_used.push_back("timeout");
	//cmd_style相关
	switch (config.cmd_style) {
		case HW_CMD_STYLE::pipe:
			cfg_used.push_back("pipe_get_input_data_exe_name");
			cfg_used.push_back("pipe_data_file");
			break;
		case HW_CMD_STYLE::redirection:
			cfg_used.push_back("redirection_data_dirname");
			break;
		case HW_CMD_STYLE::main_with_arguments:
		default:
			break;
	}

	//tc相关
	cfg_used.push_back("tc_trim");
	cfg_used.push_back("tc_lineskip");
	cfg_used.push_back("tc_lineoffset");
	cfg_used.push_back("tc_ignoreblank");
	cfg_used.push_back("tc_not_ignore_linefeed");
	cfg_used.push_back("tc_maxdiff");
	cfg_used.push_back("tc_maxline");
	cfg_used.push_back("tc_display");
	//items相关
	cfg_used.push_back("items_num");
	cfg_used.push_back("items_begin");
	cfg_used.push_back("items_end");
	switch (config.cmd_style) {
		case HW_CMD_STYLE::pipe:
			//逐个item_gname_i
			for (int i = 1; i <= config.items.items_num; ++i)
				cfg_used.push_back("item_gname_" + to_string(i));
			break;
		case HW_CMD_STYLE::redirection:
			//逐个item_fname_i
			for (int i = 1; i <= config.items.items_num; ++i)
				cfg_used.push_back("item_fname_" + to_string(i));
			break;
		case HW_CMD_STYLE::main_with_arguments:
			//逐个item_args_i
			for (int i = 1; i <= config.items.items_num; ++i)
				cfg_used.push_back("item_args_" + to_string(i));
			break;
		default:
			break;
	}

	size_t max_key_len = 0; //最大打印宽度

	//打印分割线
	messages << setfill('=') << setw(SEP_LINE_LEN) << "=" << setfill(' ') << endl;
	messages << "[" << show_name << "]配置信息如下：" << endl;
	messages << setfill('=') << setw(SEP_LINE_LEN) << "=" << setfill(' ') << endl;
	//打印数据库块（如果用到）
	if (config.exe_style != HW_EXE_STYLE::none && config.name_list_mode == HW_NAMELIST_MODE::database) {
		//数据库部分自己计算最大打印宽度
		for (size_t i = 0; i < cfg_used.size(); ++i) {
			if (cfg_used[i] == "db_host" || cfg_used[i] == "db_port" || cfg_used[i] == "db_name" ||
				cfg_used[i] == "db_username" || cfg_used[i] == "db_curr_term" || cfg_used[i] == "db_cno_list") {
				if (cfg_used[i].length() > max_key_len)
					max_key_len = cfg_used[i].length();
			}
		}
		messages << "[数据库]：" << endl;
		messages << "  " << setw(max_key_len) << "db_host" << " = " << config.database.db_host << endl;
		messages << "  " << setw(max_key_len) << "db_port" << " = " << config.database.db_port << endl;
		messages << "  " << setw(max_key_len) << "db_name" << " = " << config.database.db_name << endl;
		messages << "  " << setw(max_key_len) << "db_username" << " = " << config.database.db_username << endl;
		messages << "  " << setw(max_key_len) << "db_curr_term" << " = " << config.database.db_curr_term << endl;
		messages << "  " << setw(max_key_len) << "db_cno_list" << " = " << config.database.db_cno_list << endl;
		messages << endl;
	}
	//重新计算最大打印宽度
	max_key_len = 0;
	for (size_t i = 0; i < cfg_used.size(); ++i) {
		if (cfg_used[i].length() > max_key_len)
			max_key_len = cfg_used[i].length();
	}
	//打印主配置块
	messages << group << "：" << endl;
	for (size_t i = 0; i < cfg_used.size(); ++i) {
		const string& key = cfg_used[i];
		if (key == "db_host" || key == "db_port" || key == "db_name" || key == "db_username" || key == "db_curr_term" || key == "db_cno_list") {
			//数据库相关已打印过，跳过
			continue;
		}
		else if (key == "exe_style") {
			messages << "  " << setw(max_key_len) << key << " = " << config.exe_style_str << endl;
		}
		else if (key == "name_list") {
			messages << "  " << setw(max_key_len) << key << " = " << config.name_list << endl;
		}
		else if (key == "single_exe_dirname") {
			messages << "  " << setw(max_key_len) << key << " = " << config.single_exe_dirname << endl;
		}
		else if (key == "multi_exe_main_dirname") {
			messages << "  " << setw(max_key_len) << key << " = " << config.multi_exe_main_dirname << endl;
		}
		else if (key == "multi_exe_sub_dirname") {
			messages << "  " << setw(max_key_len) << key << " = " << config.multi_exe_sub_dirname << endl;
		}
		else if (key == "stu_exe_name") {
			messages << "  " << setw(max_key_len) << key << " = " << config.stu_exe_name << endl;
		}
		else if (key == "demo_exe_name") {
			messages << "  " << setw(max_key_len) << key << " = " << config.demo_exe_name << endl;
		}
		else if (key == "cmd_style") {
			messages << "  " << setw(max_key_len) << key << " = " << config.cmd_style_str << endl;
		}
		else if (key == "max_output_len") {
			messages << "  " << setw(max_key_len) << key << " = " << config.max_output_len << endl;
		}
		else if (key == "timeout") {
			messages << "  " << setw(max_key_len) << key << " = " << config.timeout << endl;
		}
		else if (key == "redirection_data_dirname") {
			messages << "  " << setw(max_key_len) << key << " = " << config.redirection_data_dirname << endl;
		}
		else if (key == "pipe_get_input_data_exe_name") {
			messages << "  " << setw(max_key_len) << key << " = " << config.pipe_get_input_data_exe_name << endl;
		}
		else if (key == "pipe_data_file") {
			messages << "  " << setw(max_key_len) << key << " = " << config.pipe_data_file << endl;
		}
		else if (key == "tc_trim") {//多分隔一行
			messages << endl << "  " << setw(max_key_len) << key << " = " << config.tc.tc_trim << endl;
		}
		else if (key == "tc_lineskip") {
			messages << "  " << setw(max_key_len) << key << " = " << config.tc.tc_lineskip << endl;
		}
		else if (key == "tc_lineoffset") {
			messages << "  " << setw(max_key_len) << key << " = " << config.tc.tc_lineoffset << endl;
		}
		else if (key == "tc_ignoreblank") {
			messages << "  " << setw(max_key_len) << key << " = " << config.tc.tc_ignoreblank << endl;
		}
		else if (key == "tc_not_ignore_linefeed") {
			messages << "  " << setw(max_key_len) << key << " = " << config.tc.tc_not_ignore_linefeed << endl;
		}
		else if (key == "tc_maxdiff") {
			messages << "  " << setw(max_key_len) << key << " = " << config.tc.tc_maxdiff << endl;
		}
		else if (key == "tc_maxline") {
			messages << "  " << setw(max_key_len) << key << " = " << config.tc.tc_maxline << endl;
		}
		else if (key == "tc_display") {
			messages << "  " << setw(max_key_len) << key << " = " << config.tc.tc_display << endl;
		}
		else if (key == "items_num") {//多分隔一行
			messages << endl << "  " << setw(max_key_len) << key << " = " << config.items.items_num << endl;
		}
		else if (key == "items_begin") {
			messages << "  " << setw(max_key_len) << key << " = " << config.items.items_begin << endl;
		}
		else if (key == "items_end") {
			messages << "  " << setw(max_key_len) << key << " = " << config.items.items_end << endl;
		}
		else if (key.find("item_gname_") == 0) {
			//item_gname_i
			const int index = stoi(key.substr(11));
			messages << "  " << setw(max_key_len) << "item_name_" + to_string(index) << " = " << format_group_name(config.items.item_gname[index]) << endl;
		}
		else if (key.find("item_fname_") == 0) {
			//item_fname_i
			const int index = stoi(key.substr(11));
			messages << "  " << setw(max_key_len) << "item_name_" + to_string(index) << " = " << config.items.item_fname[index] << endl;
		}
		else if (key.find("item_args_") == 0) {
			//item_args_i
			const int index = stoi(key.substr(10));
			messages << "  " << setw(max_key_len) << "item_name_" + to_string(index) << " = " << config.items.item_args[index] << endl;
		}
	}
	messages << setfill('=') << setw(SEP_LINE_LEN) << "=" << setfill(' ') << endl << endl;
}

/***************************************************************************
  函数名称：hw_checker::is_valid_cno
  功    能：检查课号格式合法性
  输入参数：cno：课号字符串
  返 回 值：true=合法；false=非法
  说    明：要求长度为8或13且必须为纯数字串
 ***************************************************************************/
bool hw_checker::is_valid_cno(const string& cno)
{
	if (!(cno.length() == 8 || cno.length() == 13))
		return false;
	if (!csu_isDigitString(cno))
		return false;
	return true;
}

/***************************************************************************
  函数名称：hw_checker::is_valid_sno
  功    能：检查学号格式合法性
  输入参数：sno：学号字符串
  返 回 值：true=合法；false=非法
  说    明：要求长度为7且必须为纯数字串
 ***************************************************************************/
bool hw_checker::is_valid_sno(const string& sno)
{
	if (sno.length() != 7)
		return false;
	if (!csu_isDigitString(sno))
		return false;
	return true;
}

/***************************************************************************
  函数名称：hw_checker::is_valid_exe_style
  功    能：判断exe_style字符串是否合法
  输入参数：s：exe_style字符串
  返 回 值：true=合法；false=非法
  说    明：合法值为none/single/multi
 ***************************************************************************/
bool hw_checker::is_valid_exe_style(const string& s)
{
	return s == "none" || s == "single" || s == "multi";
}

/***************************************************************************
  函数名称：hw_checker::parse_exe_style
  功    能：将exe_style字符串解析为枚举值
  输入参数：s：exe_style字符串
  返 回 值：对应HW_EXE_STYLE枚举；非法返回invalid
  说    明：用于配置读取与后续分支逻辑
 ***************************************************************************/
HW_EXE_STYLE hw_checker::parse_exe_style(const string& s)
{
	if (s == "none")
		return HW_EXE_STYLE::none;
	if (s == "single")
		return HW_EXE_STYLE::single;
	if (s == "multi")
		return HW_EXE_STYLE::multi;
	return HW_EXE_STYLE::invalid;
}

/***************************************************************************
  函数名称：hw_checker::is_valid_cmd_style
  功    能：判断cmd_style字符串是否合法
  输入参数：s：cmd_style字符串
  返 回 值：true=合法；false=非法
  说    明：合法值为normal/pipe/redirection/main_with_arguments
 ***************************************************************************/
bool hw_checker::is_valid_cmd_style(const string& s)
{
	return s == "normal" || s == "pipe" || s == "redirection" || s == "main_with_arguments";
}

/***************************************************************************
  函数名称：hw_checker::parse_cmd_style
  功    能：将cmd_style字符串解析为枚举值
  输入参数：s：cmd_style字符串
  返 回 值：对应HW_CMD_STYLE枚举；非法返回invalid
  说    明：用于配置读取与后续分支逻辑
 ***************************************************************************/
HW_CMD_STYLE hw_checker::parse_cmd_style(const string& s)
{
	if (s == "normal")
		return HW_CMD_STYLE::normal;
	if (s == "pipe")
		return HW_CMD_STYLE::pipe;
	if (s == "redirection")
		return HW_CMD_STYLE::redirection;
	if (s == "main_with_arguments")
		return HW_CMD_STYLE::main_with_arguments;
	return HW_CMD_STYLE::invalid;
}

/***************************************************************************
  函数名称：hw_checker::is_valid_trim
  功    能：判断配置的tc_trim字符串是否合法
  输入参数：s：tc_trim字符串
  返 回 值：true=合法；false=非法
  说    明：合法值为none/left/right/all
 ***************************************************************************/
bool hw_checker::is_valid_trim(const string& s)
{
	return s == "none" || s == "left" || s == "right" || s == "all";
}

/***************************************************************************
  函数名称：hw_checker::is_valid_display
  功    能：判断tc_display字符串是否合法
  输入参数：s：tc_display字符串
  返 回 值：true=合法；false=非法
  说    明：合法值为none/normal/detailed
 ***************************************************************************/
bool hw_checker::is_valid_display(const string& s)
{
	return s == "none" || s == "normal" || s == "detailed";
}

/***************************************************************************
  函数名称：hw_checker::get_item_col_header
  功    能：获取结果表中某测试项的表头显示内容
  输入参数：item_index：测试项序号（从1开始）
  返 回 值：表头字符串
  说    明：不同cmd_style下显示内容不同（pipe为组名、redirection为文件名等）
 ***************************************************************************/
string hw_checker::get_item_col_header(int item_index)
{
	switch (config.cmd_style) {
		case HW_CMD_STYLE::pipe:
		{
			string g = config.items.item_gname[item_index];
			format_group_name(g);
			return g;
		}

		case HW_CMD_STYLE::redirection:
			return config.items.item_fname[item_index];

		case HW_CMD_STYLE::main_with_arguments:
			return config.items.item_args[item_index];

		default:
			return to_string(item_index);
	}
}

/***************************************************************************
  函数名称：hw_checker::build_exec_cmd
  功    能：根据cmd_style构造运行命令行字符串
  输入参数：exe_fullpath：待执行exe全路径
			item_index：测试项序号
  返 回 值：可直接执行的命令行字符串
  说    明：支持normal/pipe/redirection/main_with_arguments四种方式
 ***************************************************************************/
string hw_checker::build_exec_cmd(const string& exe_fullpath, int item_index)
{
	const string exe_c = quote_if_needed(exe_fullpath);

	switch (config.cmd_style) {
		case HW_CMD_STYLE::normal:
			return exe_c;

		case HW_CMD_STYLE::pipe:
		{
			//"get_input_data 文件名 组名 | exe文件名"
			const string getter_q = quote_if_needed(config.pipe_get_input_data_exe_name);
			const string data_q = quote_if_needed(config.pipe_data_file);

			string g = config.items.item_gname[item_index];
			format_group_name(g);

			return getter_q + " " + data_q + " " + g + " | " + exe_c;
		}

		case HW_CMD_STYLE::redirection:
		{
			//"exe文件名 < 测试数据"
			const string datafile = config.redirection_data_dirname + config.items.item_fname[item_index];
			return exe_c + " < " + quote_if_needed(datafile);
		}

		case HW_CMD_STYLE::main_with_arguments:
		{
			const string args = config.items.item_args[item_index];
			if (args.empty())
				return exe_c;
			return exe_c + " " + args;
		}

		default:
			return exe_c;
	}
}

/***************************************************************************
  函数名称：hw_checker::find_student_exe_fullpath
  功    能：根据exe_style拼接学生exe全路径
  输入参数：stu：学生信息（课号/学号）
  返 回 值：学生exe全路径（找不到时返回空串）
  说    明：single与multi两种目录结构拼接规则不同
 ***************************************************************************/
string hw_checker::find_student_exe_fullpath(const HW_STUDENT_INFO& stu)
{
	if (config.exe_style == HW_EXE_STYLE::single) {
		return config.single_exe_dirname + stu.student_no + "-"
			+ stu.class_no + "-" + config.stu_exe_name;
	}

	if (config.exe_style == HW_EXE_STYLE::multi) {
		string path = config.multi_exe_main_dirname
			+ stu.student_no + "-" + stu.class_no
			+ "\\" + config.multi_exe_sub_dirname + config.stu_exe_name;
		return path;
	}

	return "";
}

/***************************************************************************
  函数名称：hw_checker::run_one_case
  功    能：运行一次测试用例并获取输出与错误码
  输入参数：exe_fullpath：待执行exe全路径
			item_index：测试项序号
			out_text：输出文本（引用输出）
			out_erno：运行错误码（引用输出）
  返 回 值：0=运行成功；非0=运行失败
  说    明：封装exe_runner；超时/输出上限等错误由exe_runner提供
 ***************************************************************************/
int hw_checker::run_one_case(const string& exe_fullpath, int item_index, string& out_text, CheckExec_Errno& out_eno)
{
	const string cmd = build_exec_cmd(exe_fullpath, item_index);
	const string exec_name = basename_from_path(exe_fullpath);

	exe_runner r(cmd, exec_name, config.max_output_len, config.timeout);
	const int rc = r.running();
	out_eno = r.get_errno();
	out_text = r.msg.str();
	return rc;
}
/*------------------公共成员函数------------------------*/

HW_DATABASE_CFG::HW_DATABASE_CFG()
{
	db_host = "";
	db_port = HW_DB_PORT_DEFAULT;
	db_name = "";
	db_username = "";
	db_passwd = "";
	db_curr_term = "";
	db_cno_list = "";
}

HW_TC_CFG::HW_TC_CFG()
{
	tc_trim = "none";
	tc_lineskip = 0;
	tc_lineoffset = 0;
	tc_ignoreblank = 0;
	tc_not_ignore_linefeed = 0;
	tc_maxdiff = 0;
	tc_maxline = 0;
	tc_display = "none";
}

HW_ITEMS_CFG::HW_ITEMS_CFG()
{
	items_num = HW_ITEMS_DEFAULT;
	items_begin = HW_ITEMS_DEFAULT;
	items_end = HW_ITEMS_DEFAULT;
	item_gname = vector<string>();
	item_fname = vector<string>();
	item_args = vector<string>();
}

/***************************************************************************
  函数名称：HW_GROUP_CFG::HW_GROUP_CFG
  功    能：初始化组配置默认值
  输入参数：无
  返 回 值：无
  说    明：用于load_config前置初始化，避免未赋值被使用
 ***************************************************************************/
HW_GROUP_CFG::HW_GROUP_CFG()
{
	group_name = "";
	include_name = "";

	database = HW_DATABASE_CFG();

	exe_style = HW_EXE_STYLE::multi;
	exe_style_str = "multi";
	single_exe_dirname = "";
	multi_exe_main_dirname = "";
	multi_exe_sub_dirname = "";
	stu_exe_name = "";
	demo_exe_name = "";

	name_list_mode = HW_NAMELIST_MODE::database;
	name_list = "";

	cmd_style = HW_CMD_STYLE::normal;
	cmd_style_str = "normal";
	pipe_get_input_data_exe_name = "";
	pipe_data_file = "";
	redirection_data_dirname = "";

	timeout = HW_TIMEOUT_DEFAULT;
	max_output_len = HW_MAX_OUTPUT_LEN_DEFAULT;

	tc = HW_TC_CFG();

	items = HW_ITEMS_CFG();
}

/***************************************************************************
  函数名称：hw_checker::hw_checker
  功    能：构造检查器对象，初始化成员变量
  输入参数：cfgfile：配置文件路径
			checkname：检查组名（不含[]或含[]由外部保证）
			debugtype：调试输出级别字符串
  返 回 值：无
  说    明：仅初始化成员，不进行实际检查
 ***************************************************************************/
hw_checker::hw_checker(const string& cfgfile, const string& checkname, const string& debugtype)
{
	cfg_file = cfgfile;
	check_name = checkname;
	debug_type = debugtype;

	checkcfg_ret = 0;

	cfg_used = vector<string>();
	messages = ostringstream();
	errors = ostringstream();

	student_list = vector<HW_STUDENT_INFO>();
	demo_outputs = vector<string>();
	stu_pass_01 = vector<vector<unsigned char>>();
	stu_eno = vector<vector<CheckExec_Errno>>();
	stu_has_exe = vector<bool>();
	result_xls_fullpath = "";
}

/***************************************************************************
  函数名称：hw_checker::~hw_checker
  功    能：析构函数
  输入参数：无
  返 回 值：无
  说    明：无动态内存，无需释放
 ***************************************************************************/
hw_checker::~hw_checker()
{
	/* 无动态内存，无需释放 */
}

/***************************************************************************
  函数名称：hw_checker::load_config
  功    能：加载配置文件并读取指定检查组配置
  输入参数：无（使用成员cfg_file/check_name）
  返 回 值：无（通过checkcfg_ret与errors反映结果）
  说    明：读取后会调用read_config_group与check_config
 ***************************************************************************/
void hw_checker::load_config()
{
	config_file_tools Cfg_tool(cfg_file, BREAK_CTYPE::Equal);

	if (Cfg_tool.is_read_succeeded() == 0) {
		cerr << "\n[--严重错误--] 无法打开配置文件[" << cfg_file << "]\n" << endl << endl;
		checkcfg_ret = -2;
		return;
	}
	const string group = "[" + check_name + "]";
	//清空并准备接收
	config = HW_GROUP_CFG();
	config.group_name = group;
	//获取所有组
	vector<string> groups;
	Cfg_tool.get_all_group(groups);
	//开始读取
	read_config_group(group, config, Cfg_tool, groups, true);
	//检查配置
	check_config();
}

/***************************************************************************
  函数名称：hw_checker::get_checkcfg_ret
  功    能：获取配置检查结果码
  输入参数：无
  返 回 值：checkcfg_ret
  说    明：0表示正常；非0表示存在错误
 ***************************************************************************/
int hw_checker::get_checkcfg_ret() const
{
	return this->checkcfg_ret;
}

/***************************************************************************
  函数名称：hw_checker::print_cfg_info
  功    能：输出配置摘要信息到标准输出
  输入参数：无
  返 回 值：无
  说    明：输出内容由store_cfg_info生成
 ***************************************************************************/
void hw_checker::print_cfg_info()
{
	cout << this->messages.str();
}

/***************************************************************************
  函数名称：hw_checker::print_errors
  功    能：输出错误信息到标准输出
  输入参数：无
  返 回 值：无
  说    明：输出errors流内容
 ***************************************************************************/
void hw_checker::print_errors()
{
	cout << this->errors.str() << endl << endl;
}

/***************************************************************************
  函数名称：hw_checker::load_student_list
  功    能：加载学生名单（来自文本文件或数据库）
  输入参数：无（使用成员config）
  返 回 值：0=成功；-1=失败（错误信息写入errors或stderr）
  说    明：exe_style为none时不加载学生；文件模式下按学号去重
 ***************************************************************************/
int hw_checker::load_student_list()
{
	student_list.clear();
	student_list.push_back(HW_STUDENT_INFO()); //占位，0号不存数据

	//exe_style = none：只测 demo，不需要学生名单
	if (config.exe_style == HW_EXE_STYLE::none) {
		return 0;
	}

	// 1) 从文本文件读
	if (config.name_list_mode == HW_NAMELIST_MODE::file) {

		ifstream fin(config.name_list.c_str(), ios::in);
		if (!fin) {
			errors << "[--严重错误--] " << get_time_str()
				<< " 文件[" << config.name_list << "]无法打开.." << endl;
			return -1;
		}

		string line;
		bool has_any_candidate_line = false; //是否存在“非空且非#”的候选行

		while (getline(fin, line)) {
			string raw = line;
			csu_trimAll(raw, false);  //去除两侧空格/tab（不忽略crlf）

			if (raw.empty())
				continue;
			if (raw[0] == '#')
				continue;

			has_any_candidate_line = true;

			//解析：至少要有 课号 学号
			istringstream iss(raw);
			string cno, sno, name;
			if (!(iss >> cno >> sno >> name)) {
				errors << get_time_str() << " 行：" << raw << "不符合要求" << endl;
				continue;
			}

			//先课号再学号
			if (!is_valid_cno(cno)) {
				errors << get_time_str() << " 行：\"" << raw << "\" 中 课号[" << cno << "]不符合要求" << endl;
				continue;
			}
			if (!is_valid_sno(sno)) {
				errors << get_time_str() << " 行：\"" << raw << "\" 中 学号[" << sno << "]不符合要求" << endl;
				continue;
			}

			//按学号去重
			bool dup = false;
			for (int i = 1; i < (int)student_list.size(); i++) {
				if (student_list[i].student_no == sno) {
					dup = true;
					break;
				}
			}
			if (dup) {
				errors << get_time_str()
					<< " 课号=" << cno
					<< " 学号=" << sno
					<< " 姓名=" << name
					<< " 重复." << endl;

				continue;
			}

			HW_STUDENT_INFO st;
			st.class_no = cno;
			st.student_no = sno;
			st.name = name;
			student_list.push_back(st);
		}
		fin.close();

		//如果全文件无候选行（全空/全注释/空行夹注释） => 严重错误：无法打开..
		if (!has_any_candidate_line) {
			errors << "[--严重错误--] " << get_time_str()
				<< " 文件[" << config.name_list << "]无法打开.." << endl;
			return -1;
		}
		return 0;
	}

	// 2) 从数据库读（数据库查到的不做正确性判断，均认为正确）
	if (config.name_list_mode == HW_NAMELIST_MODE::database) {
		MYSQL* mysql = NULL;
		MYSQL_RES* result = NULL;
		MYSQL_ROW row;

		mysql = mysql_init(NULL);
		if (mysql == NULL) {
			cerr << "mysql_init failed" << endl;
			return -1;
		}

		const char* dbserver = config.database.db_host.c_str();
		const char* dbuser = config.database.db_username.c_str();
		const char* dbpasswd = config.database.db_passwd.c_str();
		const char* dbname = config.database.db_name.c_str();
		const unsigned int dbport = (unsigned int)config.database.db_port;
		if (mysql_real_connect(mysql, dbserver, dbuser, dbpasswd, dbname, dbport, NULL, 0) == NULL) {
			cerr << "mysql_real_connect failed(" << mysql_error(mysql) << ")" << endl;
			mysql_close(mysql);
			return -1;
		}

		//设置字符集，否则读出的字符乱码
		mysql_set_character_set(mysql, "gbk");

		//查询命令
		ostringstream oss;
		const string term = config.database.db_curr_term;
		const string cno_list = config.database.db_cno_list;
		oss << "select stu_cno, stu_no, stu_name "
			<< "from view_student_for_oop "
			<< "where stu_term = '" << config.database.db_curr_term << "' "
			<< "and stu_cno in(" << config.database.db_cno_list << ") "
			<< "order by stu_no;";
		string sql = oss.str();

		//执行查询
		if (mysql_query(mysql, sql.c_str())) {
			cerr << "mysql_query failed(" << mysql_error(mysql) << ")" << endl;
			mysql_close(mysql);
			return -1;
		}

		//获取结果集
		result = mysql_store_result(mysql);
		if (result == NULL) {
			cerr << "mysql_store_result failed(" << mysql_error(mysql) << ")" << endl;
			mysql_close(mysql);
			return -1;
		}

		int col = (int)mysql_num_fields(result);

		while ((row = mysql_fetch_row(result)) != NULL) {
			HW_STUDENT_INFO stu;

			stu.class_no = (row[0] ? row[0] : "");
			stu.student_no = (row[1] ? row[1] : "");
			stu.name = (row[2] ? row[2] : "");

			student_list.push_back(stu);
		}

		mysql_free_result(result);
		mysql_close(mysql);
		return 0;
	}
	//其实不会走到这里
	cerr << "name_list_mode 非法" << endl;
	return -1;
}

/***************************************************************************
  函数名称：hw_checker::is_student_list_empty
  功    能：判断学生名单是否为空
  输入参数：无
  返 回 值：true=空（只有占位元素）；false=非空
  说    明：student_list下标0为占位
 ***************************************************************************/
bool hw_checker::is_student_list_empty() const
{
	return student_list.size() == 1;
}

/***************************************************************************
  函数名称：hw_checker::get_demo_outputs
  功    能：运行参考程序(demo)获取各测试项输出
  输入参数：无
  返 回 值：0=成功；-1=失败
  说    明：输出存入demo_outputs；同时记录start_time_str
 ***************************************************************************/
int hw_checker::get_demo_outputs()
{
	demo_outputs.clear();
	//0号位置不用。存begin到end
	demo_outputs.assign(config.items.items_end + 1, "");

	//记录开始时间
	start_time_str = get_time_str();

	for (int i = config.items.items_begin; i <= config.items.items_end; i++) {
		string output;
		CheckExec_Errno eno = CheckExec_Errno::ok;
		const int rc = run_one_case(config.demo_exe_name, i, output, eno);
		if (rc != 0) {
			cerr << "[--严重错误--] demo程序运行失败" << endl;
			return -1;
		}
		demo_outputs[i] = output;
	}

	return 0;
}

/***************************************************************************
  函数名称：hw_checker::check_all_students
  功    能：对所有学生程序执行所有测试项并比对输出
  输入参数：无
  返 回 值：0=完成（无论通过与否）
  说    明：结果写入stu_pass_01/stu_eno/stu_has_exe
 ***************************************************************************/
int hw_checker::check_all_students()
{
	int items_begin = config.items.items_begin;
	int items_end = config.items.items_end;

	//准备结果存储结构
	stu_has_exe.clear();
	stu_has_exe.assign(student_list.size() + 1, false);
	stu_pass_01.clear();
	stu_pass_01.assign(student_list.size() + 1, vector<unsigned char>(items_end + 1, '0'));
	stu_eno.clear();
	stu_eno.assign(student_list.size() + 1, vector<CheckExec_Errno>(items_end + 1, CheckExec_Errno::ok));

	//逐学生逐测试项运行并比对
	for (int s = 1; s < (int)student_list.size(); s++) {
		const HW_STUDENT_INFO& stu = student_list[s];
		const string stu_exe_fullpath = find_student_exe_fullpath(stu);

		if (!file_exists(stu_exe_fullpath)) {
			stu_has_exe[s] = 0;
			continue;
		}
		stu_has_exe[s] = true;

		//逐测试项运行
		for (int i = items_begin; i <= items_end; i++) {
			string out;
			CheckExec_Errno eno = CheckExec_Errno::ok;
			const int rc = run_one_case(stu_exe_fullpath, i, out, eno);

			stu_eno[s][i] = eno;

			if (rc != 0) {
				//运行失败：判为不通过（0）
				stu_pass_01[s][i] = '0';
				continue;
			}

			istringstream iss_demo(demo_outputs[i]);
			istringstream iss_stu(out);
			txt_compare tc(
				iss_demo, iss_stu,
				config.tc.tc_trim,
				config.tc.tc_display,
				config.tc.tc_lineskip,
				config.tc.tc_lineoffset,
				config.tc.tc_maxdiff,
				config.tc.tc_maxline,
				(config.tc.tc_ignoreblank != 0),
				(config.tc.tc_not_ignore_linefeed != 0),
				false);

			const int diff = tc.compare(true);
			stu_pass_01[s][i] = (diff == 0) ? '1' : '0';
		}
	}

	return 0;
}

/***************************************************************************
  函数名称：hw_checker::save_result_xls
  功    能：将测试结果保存为xls(制表符分隔)文件
  输入参数：无
  返 回 值：无
  说    明：文件名包含时间戳/模式信息；结束时间在此处记录
 ***************************************************************************/
void hw_checker::save_result_xls()
{
	//结束时间写在保存时刻
	end_time_str = get_time_str();

	//输出文件名：checkname + start_time
	const string xls_name = "check-result-2452769-" + format_time_for_xls(end_time_str)
		+ "-" + config.exe_style_str
		+ "-" + config.cmd_style_str
		+ "-" + (config.name_list_mode == HW_NAMELIST_MODE::file ? "txt" : "database")
		+ "-" + config.stu_exe_name
		+ ".xls";
	result_xls_fullpath = xls_name;

	ofstream fout(result_xls_fullpath.c_str(), ios::out | ios::binary);
	if (!fout) {
		cerr << "[--严重错误--] " << get_time_str() << " 结果文件[" << result_xls_fullpath << "]无法创建." << endl;
		return;
	}

	//顶部摘要（两列）
	fout << "exe_style\t" << config.exe_style_str << "\n";
	fout << "cmd_style\t" << config.cmd_style_str << "\n";
	fout << "name_list\t" << config.name_list << "\n";
	fout << "stu_exe_name\t" << config.stu_exe_name << "\n";
	fout << "statrt_time\t" << format_time_for_xls(start_time_str) << "\n";
	fout << "\n";

	//表头
	fout << "序号\t课号\t学号\t姓名\t"
		<< "正确运行\t定时器创建失败\t管道方式打开失败\t启动定时器失败\t超时\t超过输出上限\t死循环\tTC通过总数";

	const int items_begin = config.items.items_begin;
	const int items_end = config.items.items_end;

	//测试项表头
	for (int i = config.items.items_begin; i <= config.items.items_end; i++) {
		fout << "\t" << get_item_col_header(i);
	}
	fout << "\n";


	for (int s = 1; s < (int)student_list.size(); s++) {
		const HW_STUDENT_INFO& stu = student_list[s];

		const bool has_exe = stu_has_exe[s];


		int cnt_ok = 0;
		int cnt_timer_create_failed = 0;
		int cnt_popen_failed = 0;
		int cnt_timer_start_failed = 0;
		int cnt_timeout = 0;
		int cnt_max_output = 0;
		int cnt_killed = 0;
		int tc_pass_total = 0;

		if (has_exe) {
			for (int i = items_begin; i <= items_end; ++i) {

				if (stu_pass_01[s][i] == '1')
					tc_pass_total++;

				CheckExec_Errno eno = stu_eno[s][i];


				switch (eno) {
					case CheckExec_Errno::ok:
						cnt_ok++;
						break;
					case CheckExec_Errno::create_timer_id_failed:
						cnt_timer_create_failed++;
						break;
					case CheckExec_Errno::popen_faliled:
						cnt_popen_failed++;
						break;
					case CheckExec_Errno::start_timer_failed:
						cnt_timer_start_failed++;
						break;
					case CheckExec_Errno::timeout:
						cnt_timeout++;
						break;
					case CheckExec_Errno::max_output:
						cnt_max_output++;
						break;
					case CheckExec_Errno::killed_by_callback:
						cnt_killed++;
						break;
					default:
						break;
				}
			}
		}

		//每行学生结果
		fout << s << "\t"
			<< ("=text(\"" + stu.class_no + "\", \"#\")") << "\t"
			<< stu.student_no << "\t"
			<< stu.name << "\t"
			<< slash_or_int(has_exe, cnt_ok) << "\t"
			<< slash_or_int(has_exe, cnt_timer_create_failed) << "\t"
			<< slash_or_int(has_exe, cnt_popen_failed) << "\t"\
			<< slash_or_int(has_exe, cnt_timer_start_failed) << "\t"
			<< slash_or_int(has_exe, cnt_timeout) << "\t"
			<< slash_or_int(has_exe, cnt_max_output) << "\t"
			<< slash_or_int(has_exe, cnt_killed) << "\t"
			<< tc_pass_total;

		for (int i = items_begin; i <= items_end; ++i) {
			char pass = stu_pass_01[s][i];
			fout << "\t" << pass;
		}
		fout << "\n";
	}

	fout << "\n";
	fout.close();
}