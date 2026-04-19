/* 2452769 幸可函 计科 */
#pragma once

#include <iostream>
#include <fstream>
#include <string>
#include <sstream>
#include <iomanip>
#include <vector>
#include <cmath>

#include "../include/common_string_utils.h"
#include "../include/cmd_console_tools.h"

const string HIGHLIGHT_START = "\1HS\1";//高亮开始标志
const string HIGHLIGHT_END = "\1HE\1";//高亮结束标志

class txt_compare {
private:
	//新增：istringstream，用于hw_check_exe中传入字符串比较
	bool use_iss;//是否使用istringstream进行比较
	istream* p_stream1;
	istream* p_stream2;

	string filename1;
	string filename2;
	string trim_type;
	string display_type;
	int line_skip;
	int line_offset;
	int line_max_diffnum;
	int line_max_linenum;
	bool ignore_blank;
	bool not_ignore_linefeed;
	bool debug;

	ostringstream output_msg;  //用于存储比较过程中的输出信息
	int line_maxlen;  //用于存储比较过程中遇到的最长行长度，方便输出分隔行
	int diff_line_count; //用于存储比较过程中发现的不同的行数

	struct FileStatus {
		int linenum; //当前行号
		bool is_eof; //是否到达文件末尾
		EndType endtype; //当前行的结束类型
		string line; //当前行内容
		FileStatus() :linenum(0), is_eof(false), endtype(END_NONE), line("") {}
	};
	bool open_files_success;

	void get_line_maxlen(const string& filename, FileStatus& fs);
	void trim_line(string& line) const;
	bool is_blank_line(const string& line, const bool before_trim = false) const;
	void print_separator_line();
	void print_reading_tips();
	void print_diff_line(const FileStatus& fs1, const FileStatus& fs2, bool same_content, bool same_endtype, bool is_first_diff);
	void print_ruler(const int maxlen);
	void print_hex_dump(const FileStatus& fs);
	void read_next_line(istream& file, FileStatus& fs);
	void skip_line(istream& file, FileStatus& fs, const int skipcount);

public:
	txt_compare(const string& fname1, const string& fname2, const string& trimtype, const string& displaytype,
		const int lineskip, const int lineoffset, const int linemaxdiffnum, const int linemaxlinenum,
		const bool ignoreblank, const bool crcrlfnotequal, const bool debugflag);
	txt_compare(istringstream& iss1, istringstream& iss2, const string& trimtype, const string& displaytype,
		const int lineskip, const int lineoffset, const int linemaxdiffnum, const int linemaxlinenum,
		const bool ignoreblank, const bool crcrlfnotequal, const bool debugflag);
	//无动态内存申请
	~txt_compare();

	int compare(const bool silent = false);
	void result() const;
};