#
# Makefile: Makefile for encodroyd
#
# Author
#   Florian Admaksy <cit@ccc-r.de>
#

# setup files and directory names
SRC_DIR = src
RES_DIR = resources
APP_NAME = encodroyd.exe

# setup mono specific options
MCS = /usr/bin/gmcs2
MCS_FLAGES = -warn:4 -target:exe
REF = -pkg:gtk-sharp-2.0

# list of source files
SOURCES = \
        $(SRC_DIR)/*.cs\

# list of resource files
RESOURCES = \
	-resource:$(RES_DIR)/android.png\

$(APP_NAME):
	$(MCS) $(MCS_FLAGES) $(RESOURCES) $(REF) -out:$(APP_NAME) $(SOURCES)

.PHONY: clean
clean:
	rm -f *.exe