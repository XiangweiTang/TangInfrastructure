import jieba
import sys

input_path=sys.argv[1]
output_path=sys.argv[2]

input_file=open(input_path,"r", encoding="utf-8").readlines()

output_file=open(output_path,"w+",encoding="utf-8")

for line in input_file:
	seg_list=jieba.cut(line,cut_all=False)
	s=(" ".join(seg_list))
	output_file.write("%s"%s)

output_file.close()