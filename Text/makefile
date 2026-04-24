.PHONY: all clean build

all: build clean

build: 
	-pdflatex --shell-escape --interaction=nonstopmode main
	-biber main
	-pdflatex --shell-escape --interaction=nonstopmode main
	-pdflatex --shell-escape --interaction=nonstopmode main

clean: 
	rm -rf *.aux *.bcf *.log *.out *.run.xml *.toc *.bbl *.blg