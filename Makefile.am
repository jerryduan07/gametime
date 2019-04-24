AUTOMAKE_OPTIONS = foreign

install: patch-phoenix
	$(PYTHON) $(srcdir)/setup.py build install
	mkdir -p $(srcdir)/sandbox
	make create-sandbox
	make prepare-interfaces

PHOENIX_DEBUG_DIR = "$(PROGRAM_FILES)/Phoenix SDK June 2008/bin/debug"
patch-phoenix:
	cp $(srcdir)/ext/phx-bat.diff $(PHOENIX_DEBUG_DIR)
	-cd $(PHOENIX_DEBUG_DIR) && patch -N < phx-bat.diff
	rm -f $(PHOENIX_DEBUG_DIR)/phx-bat.diff
	rm -f $(PHOENIX_DEBUG_DIR)/phxvars32d.bat.orig
	rm -f $(PHOENIX_DEBUG_DIR)/phxvars32d.bat.rej
	$(PHOENIX_DEBUG_DIR)/phxvars32d.bat x86

create-sandbox:
	mkdir -p $(srcdir)/sandbox
	cp $(srcdir)/src/bin/analyzeProject.py $(srcdir)/sandbox
	cp $(srcdir)/src/bin/sampleProjectConfig.xml $(srcdir)/sandbox

prepare-interfaces:
	rm -f $(srcdir)/gametime-cli.exe
	rm -f $(srcdir)/gametime-gui.exe
	cd $(srcdir) && \
		mkshortcut "$(PYTH_SCRIPTSDIR_UNIX)/gametime-cli.exe" \
		-n gametime-cli && \
		mkshortcut "$(PYTH_SCRIPTSDIR_UNIX)/gametime-gui.exe" \
		-n gametime-gui

# Primarily for development.
find-project-configs:
	find $(srcdir) -iname "*projectConfig.xml" -print

# Primarily for development.
configure-cil:
	cd $(srcdir)/src/cil && ./configure

# Primarily for development.
patch-cil:
	-cd $(srcdir)/src/cil/src && \
		dos2unix -f rmtmps.* && \
		patch -f < rmtmps.diff
	rm -f $(srcdir)/src/cil/src/rmtmps.ml.orig
	rm -f $(srcdir)/src/cil/src/rmtmps.ml.rej
	-cd $(srcdir)/src/cil/src/ext/inliner && \
		dos2unix -f inliner.* && \
		patch -f < inliner.diff
	rm -f $(srcdir)/src/cil/src/ext/inliner/inliner.ml.orig
	rm -f $(srcdir)/src/cil/src/ext/inliner/inliner.ml.rej
#	-cd $(srcdir)/src/cil/src/frontc && \
#		dos2unix -f cabs2cil.* && \
#		patch -f < cabs2cil.diff
#	rm -f $(srcdir)/src/cil/src/frontc/cabs2cil.ml.orig
#	rm -f $(srcdir)/src/cil/src/frontc/cabs2cil.ml.rej

# Primarily for development.
cil-modules:
	cd $(CURDIR)/src/ocaml/loopHandler && \
		OCAMLPATH=$(CURDIR)/src/ocaml/lib ocamlbuild -use-ocamlfind \
		-package cil loopHandler.cma loopHandler.cmxs && \
		OCAMLFIND_DESTDIR=$(CURDIR)/src/ocaml/lib ocamlfind \
		remove loopHandler && \
		OCAMLFIND_DESTDIR=$(CURDIR)/src/ocaml/lib ocamlfind \
		install loopHandler META _build/loopHandler.cma _build/loopHandler.cmxs

# Primarily for development.
cil: patch-cil
	rm -rf $(srcdir)/tmp
	mkdir $(srcdir)/tmp
	cp $(srcdir)/src/cil/bin/cilly.bat.in $(srcdir)/tmp
	make -C $(srcdir)/src/cil
	make -C $(srcdir)/src/cil install-findlib \
		OCAMLFIND_DESTDIR=../../src/ocaml/lib
	cp $(srcdir)/tmp/cilly.bat.in $(srcdir)/src/cil/bin/cilly.bat.in
	rm -rf $(srcdir)/tmp
	-make cil-modules

# Primarily for development.
build-ocaml:
	cd $(CURDIR)/src/ocaml/loopHandler && \
		OCAMLPATH=$(CURDIR)/src/ocaml/lib ocamlbuild -use-ocamlfind \
		-package cil loopHandler.cma loopHandler.cmxs

# Primarily for development.
build-phoenix:
	make -C $(srcdir)/src/phoenix

# Primarily for development.
update-dll: build-phoenix
	cp $(srcdir)/src/phoenix/GameTime/bin/Debug/*.dll $(srcdir)/src/bin/

# Primarily for development.
clean-phoenix:
	make -C $(srcdir)/src/phoenix clean

# Primarily for development.
demo-clean:
	find $(srcdir)/demo -name "*-gt" -type d | xargs rm -rf
	find $(srcdir)/demo -name "*.pyc" | xargs rm -rf
	find $(srcdir)/demo -name "analysis" -type d | xargs rm -rf

sandbox-clean:
	find $(srcdir)/sandbox -name "*-gt" -type d | xargs rm -rf
	find $(srcdir)/sandbox -name "*.pyc" | xargs rm -rf
	find $(srcdir)/sandbox -name "analysis" -type d | xargs rm -rf

gui:
	$(PYTHON) $(srcdir)/src/gui/gui.py

# Primarily for development.
gui-dev: install
	$(PYTHON) $(srcdir)/src/gui/gui.py

# Primarily for development.
gui-clean:
	find $(srcdir)/src/gui -name "*.pyc" | xargs rm -rf

test: install
	$(PYTHON) $(srcdir)/tests/analyzeTest.py

# Primarily for development.
test-dev: update-dll test

# Primarily for development.
test-clean:
	find $(srcdir)/tests -name "*-gt" -type d | xargs rm -rf
	find $(srcdir)/tests -name "*.pyc" | xargs rm -rf

test-all: install
	$(PYTHON) $(srcdir)/tests/testHarness.py

# Primarily for development.
test-all-dev: update-dll test-all
	$(PYTHON) $(srcdir)/tests/testHarness.py

# Primarily for development.
test-all-clean: test-clean

doc-html: install
	make -C $(srcdir)/doc html

doc-latex: install
	make -C $(srcdir)/doc latex

doc-clean:
	make -C $(srcdir)/doc clean

show-doc:
	cygstart $(srcdir)/doc/build/html/index.html

# Primarily for development.
pyflakes:
	-@for filename in `find $(srcdir)/src -maxdepth 1 \
			-name "*.py" -not -name "__init__.py"` ; do \
		$(PYTH_SCRIPTSDIR_UNIX)/pyflakes.exe $$filename ; \
	done
	-@for filename in `find $(srcdir)/src/gui -name "*.py" \
			-not -name "__init__.py"` ; do \
		$(PYTH_SCRIPTSDIR_UNIX)/pyflakes.exe $$filename ; \
	done
	-@for filename in `find $(srcdir)/src/simulators -name "*.py" \
			-not -name "__init__.py"` ; do \
		$(PYTH_SCRIPTSDIR_UNIX)/pyflakes.exe $$filename ; \
	done
	-@for filename in `find $(srcdir)/src/smt -name "*.py" \
			-not -name "__init__.py"` ; do \
		$(PYTH_SCRIPTSDIR_UNIX)/pyflakes.exe $$filename ; \
	done

# Primarily for development.
pylint-src:
	-@for filename in `find $(srcdir)/src -maxdepth 1 \
			-name "*.py" -not -name "__init__.py"` ; do \
		$(PYTH_SCRIPTSDIR_UNIX)/pylint.exe $$filename \
			--rcfile=$(srcdir)/src/config-pylint.rc ; \
	done

# Primarily for development.
pylint-gui:
	-@for filename in `find $(srcdir)/src/gui -name "*.py" \
			-not -name "__init__.py"` ; do \
		$(PYTH_SCRIPTSDIR_UNIX)/pylint.exe $$filename \
			--rcfile=$(srcdir)/src/config-pylint.rc ; \
	done

# Primarily for development.
pylint-simulators:
	-@for filename in `find $(srcdir)/src/simulators -name "*.py" \
			-not -name "__init__.py"` ; do \
		$(PYTH_SCRIPTSDIR_UNIX)/pylint.exe $$filename \
			--rcfile=$(srcdir)/src/config-pylint.rc ; \
	done

# Primarily for development.
pylint-smt:
	-@for filename in `find $(srcdir)/src/smt -name "*.py" \
			-not -name "__init__.py"` ; do \
		$(PYTH_SCRIPTSDIR_UNIX)/pylint.exe $$filename \
			--rcfile=$(srcdir)/src/config-pylint.rc ; \
	done

# Primarily for development.
pylint: pylint-src pylint-gui pylint-simulators pylint-smt

# Primarily for development.
prepare-scripts:
	dos2unix -f $(srcdir)/configure
	dos2unix $(srcdir)/verify-installation
	dos2unix $(srcdir)/src/bin/analyze.in
	dos2unix $(srcdir)/src/bin/gametime-cli.bat.in
	dos2unix $(srcdir)/src/bin/gametime-gui.bat.in
	dos2unix $(srcdir)/src/bin/gui.in

# Primarily for development.
trim: prepare-scripts
	-find $(srcdir) -maxdepth 1 -name "*.py" | xargs sed -i "s/^ *$$//g"
	-find $(srcdir) -name "*.xml" | xargs sed -i "s/^ *$$//g"
	-find $(srcdir)/doc -name "*.css" | xargs sed -i "s/^ *$$//g"
	-find $(srcdir)/sandbox -name "*.py" | xargs sed -i "s/^ *$$//g"
	-find $(srcdir)/src -maxdepth 1 -name "*.in" | xargs sed -i "s/^ *$$//g"
	-find $(srcdir)/src -maxdepth 1 -name "*.py" | xargs sed -i "s/^ *$$//g"
	-find $(srcdir)/src/bin -name "*.in" | xargs sed -i "s/^ *$$//g"
	-find $(srcdir)/src/bin -name "*.py" | xargs sed -i "s/^ *$$//g"
	-find $(srcdir)/src/gui -name "*.py" | xargs sed -i "s/^ *$$//g"
	-find $(srcdir)/src/ocaml -name "*.ml" | xargs sed -i "s/^ *$$//g"
	-find $(srcdir)/src/simulators -name "*.py" | xargs sed -i "s/^ *$$//g"
	-find $(srcdir)/src/smt -name "*.py" | xargs sed -i "s/^ *$$//g"
	-find $(srcdir)/tests -name "*.py" | xargs sed -i "s/^ *$$//g"

# Primarily for development. This rule is maintained separately since it
# takes some time to complete, and should be used sparingly.
trim-tests:
	-find $(srcdir)/tests -name "*.c" | xargs sed -i "s/^ *$$//g"

# Primarily for development. This rule is used whenever a new distribution
# of GameTime needs to be created and released.
distribution: install
	python $(srcdir)/setup.py sdist --formats=gztar,zip
	cp $(srcdir)/LICENSE $(srcdir)/web/about
	cp $(srcdir)/sandbox/sampleProjectConfig.xml $(srcdir)/web/documentation
	make update-website-info

update-website-info:
	sed -e "s/[@]VERSION[@]/$(VERSION)/g" \
		$(srcdir)/web/static/php/version.php.in > \
		$(srcdir)/web/static/php/version.php

clean-local: trim
	$(PYTHON) $(srcdir)/setup.py clean
	rm -rf $(srcdir)/build
	rm -f $(srcdir)/ez_setup.pyc
	rm -rf $(srcdir)/gametime.egg-info
	rm -f $(srcdir)/gametime-cli.lnk
	rm -f $(srcdir)/gametime-gui.lnk
	rm -f $(srcdir)/sandbox/analyzeProject.py
	rm -f $(srcdir)/sandbox/sampleProjectConfig.xml
	find $(srcdir) -name "gurobi.log" | xargs rm -f
	find $(srcdir) -name "*.csprojResolveAssemblyReference.cache" | \
		xargs rm -f

distclean-local: demo-clean gui-clean test-clean

# Primarily for development.
pre-commit: pyflakes pylint distclean
